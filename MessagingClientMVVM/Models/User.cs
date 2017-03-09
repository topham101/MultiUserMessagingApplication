using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MessagingClientMVVM.Models
{
    public class User
    {
        public User(int _ID, string _name)
        {
            ID = _ID;
            Name = _name;
            IsOnline = false;
        }
        public User(int _ID, string _name, bool _isOnline)
        {
            ID = _ID;
            Name = _name;
            IsOnline = _isOnline;
        }
        public int ID { get; set; }
        public string IDstr
        {
            get
            {
                return '#' + ID.ToString("D4");
            }
        }
        public string Name { get; set; }
        public bool IsOnline { get; set; }
    }
}
