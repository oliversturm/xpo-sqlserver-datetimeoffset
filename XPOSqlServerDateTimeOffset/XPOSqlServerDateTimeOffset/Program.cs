using DevExpress.Xpo;
using DevExpress.Xpo.DB;
using DevExpress.Xpo.Metadata;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XPOSqlServerDateTimeOffset {
    public class DtoProvider : MSSqlConnectionProvider {
        public DtoProvider(IDbConnection connection, AutoCreateOption autoCreateOption)
          : base(connection, autoCreateOption) {
        }

        protected override object ReformatReadValue(object value, ReformatReadValueArgs args) {
            // This implementation deactivates the default behavior of the base 
            // class logic, because the conversion step is not necessary for the type
            // DateTimeOffset, and because the attempt at conversion
            // results in exceptions since there is no automatic conversion mechanism.
            if (value != null) {
                Type valueType = value.GetType();
                if (valueType == typeof(DateTimeOffset))
                    return value;
            }
            return base.ReformatReadValue(value, args);
        }
    }

    public class TimeData : XPObject {
        public TimeData(Session session)
          : base(session) { }

        private string name;
        public string Name {
            get { return name; }
            set { SetPropertyValue("Name", ref name, value); }
        }

        DateTimeOffset dto;
        [ValueConverter(typeof(DateTimeOffsetConverter))]
        [DbType("datetimeoffset")]
        public DateTimeOffset Dto {
            get { return dto; }
            set { SetPropertyValue("Dto", ref dto, value); }
        }
    }

    public class DateTimeOffsetConverter : ValueConverter {
        public override object ConvertFromStorageType(object value) {
            // We're ignoring the request to convert here, knowing that the loaded
            // object is already the correct type because SqlClient returns it 
            // that way.
            return value;
        }

        public override object ConvertToStorageType(object value) {
            if (value is DateTimeOffset) {
                var dto = (DateTimeOffset)value;
                return dto.ToString();
            }
            else
                return value;
        }

        public override Type StorageType {
            get { return typeof(string); }
        }
    }

    class Program {
        static void Main(string[] args) {
            XpoDefault.DataLayer =
              new SimpleDataLayer(new DtoProvider(
                  new SqlConnection("data source=.\\SQLEXPRESS;integrated security=SSPI;Type System Version=SQL Server 2012;initial catalog=XPOSqlServerDateTimeOffset"),
                AutoCreateOption.DatabaseAndSchema));

            using (UnitOfWork uow = new UnitOfWork()) {
                uow.ClearDatabase();
                uow.UpdateSchema(typeof(TimeData));
            }

            using (UnitOfWork uow = new UnitOfWork()) {
                new TimeData(uow) {
                    Name = "Test 1",
                    Dto = DateTimeOffset.Now
                }.Save();
                uow.CommitChanges();
            }

            using (UnitOfWork uow = new UnitOfWork()) {
                var data = new XPCollection<TimeData>();
                foreach (var d in data) {
                    Console.WriteLine("Name: " + d.Name);
                    Console.WriteLine("Dto: " + d.Dto);
                }
            }
        }
    }
}