using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestApplication
{
    [Serializable]
    public class Product
    {
        public Product() { }

        public virtual int Id
        {
            set;
            get;
        }

        public Decimal UnitPrice
        {
            get;
            set;
        }

        public virtual string Name
        {
            set;
            get;
        }

        public virtual string ClassName
        {
            set;
            get;
        }

        public virtual string Category
        {
            set;
            get;
        }

        public override string ToString()
        {
            StringBuilder builder = new StringBuilder();
            builder.Append("[");

            builder.Append("Name ");
            builder.Append(Name);

            builder.Append(", UnitPrice ");
            builder.Append(UnitPrice);

            builder.Append("]");

            return builder.ToString();
        }
    }
}
