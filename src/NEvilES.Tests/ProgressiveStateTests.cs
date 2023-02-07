using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;

namespace NEvilES.Tests
{
    public class ProgressiveStateTests
    {
        public ProgressiveStateTests()
        {
            
        }

        [Fact]
        public void Set_Property()
        {
            var path = "MainApplicant.Name";
            var value = "Bunnings";

            var json = JsonConvert.SerializeObject(Application.NewEmpty());
            var jo = Set(path, value, json);

            var app = jo.ToObject<Application>()!;

            json = JsonConvert.SerializeObject(app);
            var x = (JObject.Parse(json)!.SelectToken(path)! as JValue)!;

            Assert.Equal(value,app.MainApplicant.Name);
            Assert.Equal(value,x.Value);
        }

        //[Fact]
        public void Add_Array()
        {
            var json = JsonConvert.SerializeObject(Application.NewEmpty());
            var jo = Set("Assets", "+", json);

            var app = jo.ToObject<Application>()!;

            Assert.NotEmpty(app.Assets);
        }

        private static JObject Set(string path, string value, string json)
        {
            var jo = JObject.Parse(json);

            var t = jo.SelectToken(path)!;

            switch (t.Type)
            {
                case JTokenType.None:
                    break;
                case JTokenType.Object:
                case JTokenType.Null:
                case JTokenType.Guid:
                case JTokenType.Integer:
                case JTokenType.String:
                case JTokenType.Boolean:
                    var jv = (JValue)t!;
                    jv.Value = value;
                    break;
                case JTokenType.Array:
                    ((JArray)t).Add(new JObject(new Asset()));
                    break;
                case JTokenType.Constructor:
                    break;
                case JTokenType.Property:
                    break;
                case JTokenType.Comment:
                    break;
                case JTokenType.Float:
                    break;
                case JTokenType.Undefined:
                    break;
                case JTokenType.Date:
                    break;
                case JTokenType.Raw:
                    break;
                case JTokenType.Bytes:
                    break;
                case JTokenType.Uri:
                    break;
                case JTokenType.TimeSpan:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return jo;
        }
    }

    public class Application
    {
        public static Application NewEmpty()
        {
            return new Application();
        }

        public Application()
        {
            Submitted = null;
            MainApplicant = new MainApplicant();
            Applicants = new List<Applicant>();
            Assets = new List<Asset>();
            Schedule = new FinancialSchedule();
        }

        public DateTime? Submitted { get; set; }
        public MainApplicant MainApplicant { get; set; }
        public List<Applicant> Applicants { get; set; }
        public List<Asset> Assets { get; set; }
        public FinancialSchedule Schedule { get; set; }
    }

    public class FinancialSchedule
    {
    }

    public class Applicant
    {
    }

    public class Asset
    {
        public AssetType Type { get; set; }

        public Supplier Supplier { get; set; }
    }

    public enum AssetType
    {
        Primary,
        Secondary,
        Tertiary,
    }

    public class Supplier
    {
    }

    public class MainApplicant
    {
        public string Name { get; set; }
        public string ABN { get; set; }
    }
}