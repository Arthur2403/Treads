using System;
using System.Threading;

namespace Treads
{
    class Program
    {
        private static Thread _primeThread;
        private static Thread _fibonacciThread;
        private static CancellationTokenSource _primeCts;
        private static CancellationTokenSource _fibonacciCts;
        private static ManualResetEvent _primePauseEvent = new ManualResetEvent(true);
        private static ManualResetEvent _fibonacciPauseEvent = new ManualResetEvent(true);
        private static readonly object consoleLock = new object();

        static void Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            Console.InputEncoding = System.Text.Encoding.UTF8;

            while (true)
            {
                Console.WriteLine("Введіть нижню межу для простих чисел (за замовчуванням 2) або 'exit' для завершення:");
                string primeLowerInput = Console.ReadLine();
                if (primeLowerInput.ToLower() == "exit") break;

                Console.WriteLine("Введіть верхню межу для простих чисел (порожнє для нескінченності):");
                string primeUpperInput = Console.ReadLine();

                Console.WriteLine("Введіть нижню межу для чисел Фібоначчі (за замовчуванням 0):");
                string fibLowerInput = Console.ReadLine();

                Console.WriteLine("Введіть верхню межу для чисел Фібоначчі (порожнє для нескінченності):");
                string fibUpperInput = Console.ReadLine();

                long primeLower = 2;
                long? primeUpper = null;
                long fibLower = 0;
                long? fibUpper = null;

                if (!string.IsNullOrWhiteSpace(primeLowerInput))
                {
                    if (!long.TryParse(primeLowerInput, out primeLower) || primeLower < 2)
                    {
                        Console.WriteLine("Нижня межа для простих чисел повинна бути цілим числом >= 2");
                        continue;
                    }
                }

                if (!string.IsNullOrWhiteSpace(primeUpperInput))
                {
                    if (!long.TryParse(primeUpperInput, out long upperValue) || upperValue < primeLower)
                    {
                        Console.WriteLine("Верхня межа для простих чисел повинна бути цілим числом >= нижньої межі");
                        continue;
                    }
                    primeUpper = upperValue;
                }

                if (!string.IsNullOrWhiteSpace(fibLowerInput))
                {
                    if (!long.TryParse(fibLowerInput, out fibLower) || fibLower < 0)
                    {
                        Console.WriteLine("Нижня межа для чисел Фібоначчі повинна бути цілим числом >= 0");
                        continue;
                    }
                }

                if (!string.IsNullOrWhiteSpace(fibUpperInput))
                {
                    if (!long.TryParse(fibUpperInput, out long upperValue) || upperValue < fibLower)
                    {
                        Console.WriteLine("Верхня межа для чисел Фібоначчі повинна бути цілим числом >= нижньої межі");
                        continue;
                    }
                    fibUpper = upperValue;
                }

                _primeCts = new CancellationTokenSource();
                _fibonacciCts = new CancellationTokenSource();
                _primePauseEvent.Reset();
                _fibonacciPauseEvent.Reset();
                _primeThread = new Thread(() => GeneratePrimes(primeLower, primeUpper, _primeCts.Token, _primePauseEvent));
                _fibonacciThread = new Thread(() => GenerateFibonacci(fibLower, fibUpper, _fibonacciCts.Token, _fibonacciPauseEvent));
                _primeThread.Start();
                _fibonacciThread.Start();
                _primePauseEvent.Set();
                _fibonacciPauseEvent.Set();

                Console.WriteLine("Натисніть 'p' для зупинки простих чисел, 'P' для призупинення, 'r' для відновлення;");
                Console.WriteLine("Натисніть 'f' для зупинки Фібоначчі, 'F' для призупинення, 'R' для відновлення;");
                Console.WriteLine("або Enter для зупинки обох:");

                while (_primeThread.IsAlive || _fibonacciThread.IsAlive)
                {
                    if (Console.KeyAvailable)
                    {
                        char key;
                        lock (consoleLock)//Я знаю що на цю домашню ще не вчили але без цього вести нічого не можна
                        {
                            key = Console.ReadKey(true).KeyChar;
                        }
                        if (key == 'p' && _primeThread.IsAlive)
                        {
                            lock (consoleLock)
                            {
                                _primeCts.Cancel();
                                _primeThread.Join();
                                Console.WriteLine("Потік простих чисел зупинено.");
                            }
                        }
                        else if (key == 'P' && _primeThread.IsAlive)
                        {
                            lock (consoleLock)
                            {
                                _primePauseEvent.Reset();
                                Console.WriteLine("Потік простих чисел призупинено.");
                            }
                        }
                        else if (key == 'r' && _primeThread.IsAlive)
                        {
                            lock (consoleLock)
                            {
                                _primePauseEvent.Set();
                                Console.WriteLine("Потік простих чисел відновлено.");
                            }
                        }
                        else if (key == 'f' && _fibonacciThread.IsAlive)
                        {
                            lock (consoleLock)
                            {
                                _fibonacciCts.Cancel();
                                _fibonacciThread.Join();
                                Console.WriteLine("Потік чисел Фібоначчі зупинено.");
                            }
                        }
                        else if (key == 'F' && _fibonacciThread.IsAlive)
                        {
                            lock (consoleLock)
                            {
                                _fibonacciPauseEvent.Reset();
                                Console.WriteLine("Потік чисел Фібоначчі призупинено.");
                            }
                        }
                        else if (key == 'R' && _fibonacciThread.IsAlive)
                        {
                            lock (consoleLock)
                            {
                                _fibonacciPauseEvent.Set();
                                Console.WriteLine("Потік чисел Фібоначчі відновлено.");
                            }
                        }
                        else if (key == (char)13)
                        {
                            lock (consoleLock)
                            {
                                _primeCts.Cancel();
                                _fibonacciCts.Cancel();
                                _primeThread.Join();
                                _fibonacciThread.Join();
                                Console.WriteLine("Обидва потоки зупинено.");
                            }
                            break;
                        }
                    }
                    else
                    {
                        Thread.Sleep(100);
                    }
                }
            }
        }

        static void GeneratePrimes(long lower, long? upper, CancellationToken token, ManualResetEvent pauseEvent)
        {
            long current = lower;
            while (!token.IsCancellationRequested && (!upper.HasValue || current <= upper.Value))
            {
                pauseEvent.WaitOne();
                if (IsPrime(current))
                {
                    lock (consoleLock)
                    {
                        Console.WriteLine($"Prime: {current}");
                    }
                    Thread.Sleep(1000);
                }
                current++;
            }
        }

        static void GenerateFibonacci(long lower, long? upper, CancellationToken token, ManualResetEvent pauseEvent)
        {
            long a = 0, b = 1;
            while (!token.IsCancellationRequested && (!upper.HasValue || b <= upper.Value))
            {
                pauseEvent.WaitOne();
                if (b >= lower && IsFibonacci(b))
                {
                    lock (consoleLock)
                    {
                        Console.WriteLine($"Fibonacci: {b}");
                    }
                    Thread.Sleep(1000);
                }
                long temp = a + b;
                a = b;
                b = temp;
            }
        }

        static bool IsPrime(long number)
        {
            if (number <= 1) return false;
            if (number <= 3) return true;
            if (number % 2 == 0 || number % 3 == 0) return false;

            for (long i = 5; i * i <= number; i += 6)
            {
                if (number % i == 0 || number % (i + 2) == 0) return false;
            }
            return true;
        }

        static bool IsFibonacci(long number)
        {
            long test1 = 5 * number * number + 4;
            long test2 = 5 * number * number - 4;
            return IsPerfectSquare(test1) || IsPerfectSquare(test2);
        }

        static bool IsPerfectSquare(long number)
        {
            long sqrt = (long)Math.Sqrt(number);
            return sqrt * sqrt == number;
        }
    }
}