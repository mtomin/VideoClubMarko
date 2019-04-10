using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VideoKlub
{
    class AddUserParameters
    {
        public AddUserParameters()
        {

        }
        public string FirstName{ get; set; }
        public string LastName { get; set; }
        public string StreetAddress { get; set; }
        public string City { get; set; }
        public string PostalCode { get; set; }
    }
}
