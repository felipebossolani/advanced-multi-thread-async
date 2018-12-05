using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace advanced_multi_trhead_async_parallel
{
    class Program
    {
        //ThreadLocal is used in process 4: Global Variables
        public static ThreadLocal<int> _field =
            new ThreadLocal<int>(() =>
            {
                return Thread.CurrentThread.ManagedThreadId;
            });


        static void Main(string[] args)
        {
            Console.WriteLine(@"Creator: Felipe Bossolani - fbossolani[at]gmail.com");
            Console.WriteLine(@"Examples based on: http://returnsmart.blogspot.com/2015/06/mcsd-programming-in-c-part-1-70-483.html");
            Console.WriteLine("Choose a Thread Method: ");
            Console.WriteLine("01- Regular Thread");
            Console.WriteLine("02- Parameterized Thread");
            Console.WriteLine("03- Aborted Thread");
            Console.WriteLine("04- Global Variables Thread");
            Console.WriteLine("05- Simple Task");
            Console.WriteLine("06- Return Value from Tasks");
            Console.WriteLine("07- Simple Continuation Task");
            Console.WriteLine("08- Overload Continuation Task");
            Console.WriteLine("09- Child Tasks");
            Console.WriteLine("10- Child Tasks with TaskFactory");
            Console.WriteLine("11- WaitAny Task");
            Console.WriteLine("12- WaitAll Tasks");
            Console.WriteLine("13- Cancel Task");
            
            int option = 0;
            int.TryParse(Console.ReadLine(), out option);

            switch (option)
            {
                case 1:
                    { 
                        RegularThread();
                        break;
                    }
                case 2:
                    {
                        ParameterizedThread();
                        break;
                    }
                case 3:
                    {
                        AbortedThread();
                        break;
                    }
                case 4:
                    {
                        GlobalVariablesThread();
                        break;
                    }
                case 5:
                    {
                        SimpleTask();
                        break;
                    }
                case 6:
                    {
                        ReturnValueTask();
                        break;
                    }
                case 7:
                    {
                        ContinuationWithTask();
                        break;
                    }
                case 8:
                    {
                        OverloadsContinueWithTask();
                        break;
                    }
                case 9:
                    {
                        ChildTasks();
                        break;
                    }
                case 10:
                    {
                        ChildTaskFactory();
                        break;
                    }
                case 11:
                    {
                        WaitAnyTask();
                        break;
                    }
                case 12:
                    {
                        WaitAllTasks();
                        break;
                    }
                case 13:
                    {
                        CancelTask();
                        break;
                    }
                default:
                    {
                        Console.WriteLine("Invalid option...");
                        break;
                    }
            }            
        }

        private static void CancelTask()
        {
            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
            CancellationToken token = cancellationTokenSource.Token;

            Task task = Task.Run(() =>
            {
                string symbol = "*";
                while (!token.IsCancellationRequested)
                {
                    Console.Write(symbol);
                    Thread.Sleep(500);
                    symbol = symbol.Equals("*") ? "-" : "*";

                }

                token.ThrowIfCancellationRequested();
            }, token);

            try
            {
                Console.WriteLine("Press enter to stop the task");
                Console.ReadLine();

                cancellationTokenSource.Cancel();
                task.Wait();
            }
            catch (AggregateException e)
            {
                Console.WriteLine(e.InnerExceptions[0].Message);
            }
        }

        private static void WaitAnyTask()
        {
            Task<int>[] tasks = new Task<int>[3];
            tasks[0] = Task.Run(() => { Thread.Sleep(2000); return 1; });
            tasks[1] = Task.Run(() => { Thread.Sleep(1000); return 2; });
            tasks[2] = Task.Run(() => { Thread.Sleep(3000); return 3; });

            while (tasks.Length > 0)
            {
                int i = Task.WaitAny(tasks);
                Task<int> completedTask = tasks[i];

                Console.WriteLine("Finished task #{0}", completedTask.Result);

                var temp = tasks.ToList();
                temp.RemoveAt(i);
                tasks = temp.ToArray();
            }
        }

        private static void WaitAllTasks()
        {
            Task<int>[] tasks = new Task<int>[3];
            tasks[0] = Task.Run(() => { Thread.Sleep(2000); Console.WriteLine("Finished Task 1"); return 1; });
            tasks[1] = Task.Run(() => { Thread.Sleep(1000); Console.WriteLine("Finished Task 2"); return 2; });
            tasks[2] = Task.Run(() => { Thread.Sleep(3000); Console.WriteLine("Finished Task 3"); return 3; });

            Task.WaitAll(tasks);
            Console.WriteLine("All tasks are finished!");
            
        }

        private static void ChildTaskFactory()
        {
            Task<Int32[]> parent = Task.Run(() =>
            {
                var results = new Int32[3];

                TaskFactory tf = new TaskFactory(TaskCreationOptions.AttachedToParent, TaskContinuationOptions.ExecuteSynchronously);

                tf.StartNew(() => results[0] = 10);
                tf.StartNew(() => results[1] = 100);
                tf.StartNew(() => results[2] = 1000);

                return results;
            });

            var finalTask = parent.ContinueWith(
                parentTask =>
                {
                    foreach(var i in parentTask.Result)
                        Console.WriteLine(i);
                });
            finalTask.Wait();
        }

        private static void ChildTasks()
        {
            Task<Int32[]> parent = Task.Run(() =>
            {
                var results = new Int32[3];
                new Task(() => results[0] = 1, TaskCreationOptions.AttachedToParent).Start();
                new Task(() => results[1] = 10, TaskCreationOptions.AttachedToParent).Start();
                new Task(() => results[2] = 100, TaskCreationOptions.AttachedToParent).Start();
                
                return results;
            });

            var finalTask = parent.ContinueWith(
                parentTask =>
                {
                    foreach (var i in parentTask.Result)
                    {
                        Console.WriteLine(i);
                    }
                });
            finalTask.Wait();
        }

        private static void OverloadsContinueWithTask()
        {
            Task<int> t = Task.Run(() =>
            {
                int i = 0;
                //uncomment below if you want to simulate faulted method
                // i = i / i;
                return 42;
            });

            t.ContinueWith((i) =>
            {
                Console.WriteLine("Canceled");
            }, TaskContinuationOptions.OnlyOnCanceled);

            t.ContinueWith((i) =>
            {
                Console.WriteLine("Faulted");
            }, TaskContinuationOptions.OnlyOnFaulted);

            var completedTask = t.ContinueWith((i) =>
            {
                Console.WriteLine("Completed");
            }, TaskContinuationOptions.OnlyOnRanToCompletion);
            completedTask.Wait();
            Console.WriteLine(t.Result);

        }


        private static void ContinuationWithTask()
        {
            Task<int> t = Task.Run(() =>
            {
                return DateTime.Now.Day;
            }).ContinueWith((i) =>
            {
                var year = DateTime.Now.Year;
                var month = DateTime.Now.Month;
                var eom = new DateTime(year, month, DateTime.DaysInMonth(year, month));

                return eom.Day - i.Result;
            });

            Console.WriteLine("{0} day(s) left in this month", t.Result);
        }

        private static void ReturnValueTask()
        {
            Task<int> t = Task.Run(() =>
            {
                return DateTime.Now.Day;
            });
            Console.WriteLine("Today day number is: {0}", t.Result);
        }

        private static void SimpleTask()
        {
            
            Task t = Task.Run(() =>
            {
                Console.Write("*");
                for (int x = 0; x < 100; x++)
                {
                    Console.Write("*");
                }
                Console.WriteLine("");
            });
            t.Wait();
        }

        private static void GlobalVariablesThread()
        {
            Thread t1 = new Thread(() =>
            {
                for (int i = 0; i < _field.Value; i++)
                    Console.WriteLine("Thread T1: {0}", i);
            });
            t1.Start();

            Thread t2 = new Thread(() =>
            {
                for (int i = 0; i< _field.Value; i++)
                    Console.WriteLine("Thread T2 {0}", i);
            }
            );
            t2.Start();
        }

        private static void AbortedThread()
        {
            bool stopeed = false;
            Thread t = new Thread(new ThreadStart(() =>
            {
                int i = 0;
                while (!stopeed)
                {
                    Console.WriteLine("Running {0}...", i);
                    Thread.Sleep(500);
                    i++;
                }
                Console.WriteLine("Thread aborted...");
            }));

            t.Start();
            Console.WriteLine("Press any key to abort the thread");
            Console.ReadKey();

            stopeed = true;

            t.Join();
        }

        private static void RegularThread()
        {
            Thread t = new Thread(new ThreadStart(ThreadMethod));
            //t.IsBackground = true;
            t.Start();

            for (int i = 0; i < 4; i++)
            {
                Console.WriteLine("Main thread: Do some work: {0}", i);
                Thread.Sleep(0);
            }
            t.Join();
        }

        private static void ParameterizedThread()
        {
            Thread t = new Thread(new ParameterizedThreadStart(ThreadMethod));
            //t.IsBackground = true;
            t.Start(5);

            for (int i = 0; i < 4; i++)
            {
                Console.WriteLine("Main thread: Do some work: {0}", i);
                Thread.Sleep(0);
            }
            t.Join();
        }

        public static void ThreadMethod() {
            for (int i = 0; i < 10; i++)
            {
                Console.WriteLine("ThreadProc: {0}", i);
                Thread.Sleep(0);
            }
        }

        public static void ThreadMethod(object o)
        {
            for (int i = 0; i < (int)o; i++)
            {
                Console.WriteLine("ThreadProc(object): {0}", i);
                Thread.Sleep(0);
            }
        }
    }
}
