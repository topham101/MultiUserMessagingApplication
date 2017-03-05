using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MessagingServer
{
    // Code originally from: http://stackoverflow.com/a/9995339 by user "JaredPar"
    // Expanded on by me.
    public class ThreadSafeList<T>
    {
        private List<T> _list = new List<T>();
        private object _sync = new object();
        public void Add(T value)
        {
            lock (_sync)
            {
                _list.Add(value);
            }
        }
        public T Find(Predicate<T> predicate)
        {
            lock (_sync)
            {
                return _list.Find(predicate);
            }
        }
        public T FirstOrDefault()
        {
            lock (_sync)
            {
                return _list.FirstOrDefault();
            }
        }
        public bool Remove(T searchQuery)
        {
            lock (_sync)
            {
                return _list.Remove(searchQuery);
            }
        }
    }
}
