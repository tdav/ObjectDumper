using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ObjectDumperConsoleApp.Model
{
    public class ModelDateTime
    {
        public DateTime? Date { get; set; }

        /// <summary>
        /// Санадан
        /// </summary>   
        private DateTime? dateFrom;
        public DateTime? DateFrom
        {
            get { return ((this.dateFrom != null) ? this.dateFrom : this.dateTill); }
            set { this.dateFrom = value; }
        }
        /// <summary>
        /// Санагача
        /// </summary>   
        private DateTime? dateTill;
        public DateTime? DateTill
        {
            get { return ((this.dateTill != null) ? this.dateTill : this.dateFrom); }
            set { this.dateTill = value; }
        }

        public string Value
        {
            get { return ((this.Date == null) || (this.Date == DateTime.MinValue)) ? " - " : this.Date.Value.ToString(); }
        }

        public string FullValue
        {
            get { return ((this.Date == null) || (this.Date == DateTime.MinValue)) ? " - " : this.Date.Value.ToString("dd.MM.yyyy HH:mm:ss"); }
        }

        public ModelDateTime(DateTime? inDateFrom, DateTime? inDateTill)
        {
            this.dateFrom = inDateFrom;
            this.dateTill = inDateTill;
        }

        public bool IsEmpty()
        {
            return ((this.Date == null) || (this.Date == DateTime.MinValue));
        }

        public bool IsNull()
        {
            return ((this.DateFrom == null) && (this.DateTill == null));
        }

        public bool IsNotNull()
        {
            return (this.DateFrom != null) || (this.DateTill != null);
        }

        public ModelDateTime() { }

        public ModelDateTime(DateTime? inDate)
        {
            this.Date = inDate;
            this.DateFrom = this.DateTill = null;
        }

        public ModelDateTime(DateTime inDate)
        {
            this.Date = inDate;
            this.DateFrom = this.DateTill = null;
        }

        public static implicit operator ModelDateTime(DateTime? date) => new ModelDateTime(date);
        public static implicit operator ModelDateTime(DateTime date) => new ModelDateTime(date);

        public override string ToString()
        {
            return ((this.Date == null) || (this.Date == DateTime.MinValue)) ? " - " : this.Date.Value.ToString("dd.MM.yyyy hh:mm:ss");
        }
    }
}
