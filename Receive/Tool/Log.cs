using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;

namespace Receive.Tool
{
    /// <summary>
    /// 日志类
    /// </summary>
    public class Log : IDisposable
    {
        // 日志对象的缓存队列
        private readonly Queue<Msg> msgs;
        // 日志文件保存的路径
        private readonly string path;
        // 不要用静态，只针对当前文件进行锁定
        private readonly object locker;
        private readonly Thread thread;

        /// <summary>
        /// 创建日志对象的新实例，根据指定的日志文件路径和指定的日志文件创建类型
        /// </summary>
        /// <param name="p">日志文件保存路径</param>
        public Log(string p)
        {
            path = p;
            msgs = new Queue<Msg>();
            locker = new object();
            thread = new Thread(Work);
            thread.Start();
        }

        //日志文件写入线程执行的方法
        private void Work()
        {
            while (true)
            {
                //判断队列中是否存在待写入的日志
                if (msgs != null && msgs.Count > 0)
                {
                    lock (locker)
                    {
                        Msg msg = msgs.Peek();
                        if (msg != null && FileWrite(msg))
                        {
                            // 写入成功
                            msgs.Dequeue();
                        }
                        else
                        {
                            Thread.Sleep(1);
                        }
                    }
                }
                else
                {
                    Thread.Sleep(1);
                }
            }
        }

        //写入日志文本到文件的方法
        private bool FileWrite(Msg msg)
        {
            try
            {
                using (var writer = new StreamWriter(path, true, Encoding.UTF8))
                {
                    writer.WriteLine($"【{msg.DateTime}】\t{msg.Text}");
                    writer.Flush();
                    Console.WriteLine($"写入日志：{msg.Text}");
                    return true;
                }
            }
            catch (Exception e)
            {
                // 文件被占用，等待下次在写入
                Console.Out.WriteLine($"文件被占用 {msg.Text}，本次跳过输出：{e.Message}");
                return false;
            }
        }

        public void Write(string msg)
        {
            if (msg != null)
            {
                lock (msgs)
                {
                    var model = new Msg()
                    {
                        DateTime = DateTime.Now,
                        Text = msg
                    };
                    msgs.Enqueue(model);
                }
            }
        }

        public void Dispose()
        {
            // 等待日志输出完毕
            while (msgs.Count > 0)
            {
                Thread.Sleep(1);
            }

            thread.Abort();
        }

        class Msg
        {
            public DateTime DateTime { get; set; }
            public string Text { get; set; }
        }
    }

}