using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MessagingClientMVVM.Models
{
    class User
    {
        public User(int _ID, string _name)
        {
            ID = _ID;
            Name = _name;
        }
        public int ID { get; set; }
        public string Name { get; set; }
    }
}
