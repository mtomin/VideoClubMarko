using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VideoKlub
{
    class AddMovieParameters
    {
        public string Title { get; set; }
        public string Director { get; set; }
        public int NumberOfCopies { get; set; }
        public int Year { get; set; }
        public int? Runtime { get; set; }
    }
}
