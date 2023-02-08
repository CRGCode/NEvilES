using System;
using System.Collections;
using System.Collections.Generic;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NEvilES.Abstractions.ObjectPath;
using NEvilES.Abstractions.ObjectPath.PathElements;
using NEvilES.Tests.ObjectPath.Helpers;

namespace NEvilES.Tests.ObjectPath
{
    [TestClass]
    public class ResolveTests
    {
        [TestMethod]
        public void SinglePropertyResolution_CorrectSetup_Success()
        {
            var value = "1";
            var r = new Resolver();
            var o = new { Property = value };
            var path = "Property";

            var result = (Property)r.Resolve(o, path);
            result.Value.Should().Be(value);
        }

        [TestMethod]
        public void SinglePropertyResolution_BaseClass_Success()
        {
            var value = "1";
            var r = new Resolver();
            var o = new ChildClass { Property1 = value };
            var path = "Property1";

            var result = (Property)r.Resolve(o, path);
            result.Value.Should().Be(value);
        }

        [TestMethod]
        public void MultiplePropertyResolution_CorrectSetup_Success()
        {
            var value = "1";
            var r = new Resolver();
            var o = new { Property1 = new { Property2 = value } };
            var path = "Property1.Property2";

            var result = (Property)r.Resolve(o, path);
            result.Value.Should().Be(value);
        }

        [TestMethod]
        public void MultiplePropertyResolution_Null_ShouldThrow()
        {
            var r = new Resolver();
            var o = new { Property1 = (string)null };
            var path = "Property1.Property2";

            r.Invoking(re => re.Resolve(o, path)).Should().Throw<NullReferenceException>();
        }

        [TestMethod]
        public void DictionaryKeyResolutionWithProperty_CorrectSetup_Success()
        {
            var r = new Resolver();
            var dictionary = new Dictionary<string, string> { { "Key", "Value" } };
            var o = new { Dictionary = dictionary };
            var path = "Dictionary[Key]";

            var result = r.Resolve(o, path);
            result.Should().Be("Value");
        }

        [TestMethod]
        public void DictionaryKeyResolution_CorrectSetup_Success()
        {
            var r = new Resolver();
            var dictionary = new Dictionary<string, string> { { "Key", "Value" } };
            var path = "[Key]";

            var result = r.Resolve(dictionary, path);
            result.Should().Be("Value");
        }

        [TestMethod]
        public void MultipleDictionaryKeyResolution_CorrectSetup_Success()
        {
            var r = new Resolver();
            var dictionary = new Dictionary<string, Dictionary<string, string>> {
                { "Key", new Dictionary<string, string> { { "Key", "Value" } } }
            };
            var path = "[Key][Key]";

            var result = r.Resolve(dictionary, path);
            result.Should().Be("Value");
        }

        [TestMethod]
        public void ArrayIndexResolutionWithProperty_CorrectSetup_Success()
        {
            var r = new Resolver();
            var array = new[] { "1", "2" };
            var o = new { Array = array };
            var path = "Array[0]";

            var result = r.Resolve(o, path);
            result.Should().Be("1");
        }

        [TestMethod]
        public void ArrayIndexResolution_CorrectSetup_Success()
        {
            var r = new Resolver();
            var array = new[] { "1", "2" };
            var path = "[0]";

            var result = r.Resolve(array, path);
            result.Should().Be("1");
        }

        [TestMethod]
        public void MultipleArrayIndexResolution_CorrectSetup_Success()
        {
            var r = new Resolver();
            var array = new[] { new[] { "1", "2" } };
            var path = "[0][0]";

            var result = r.Resolve(array, path);
            result.Should().Be("1");
        }

        [TestMethod]
        public void SelectionResolution_CorrectSetup_Success()
        {
            var r = new Resolver();
            var array = new[] { 1, 2 };
            var path = "[]";

            var result = r.Resolve(array, path);
            result.Should().BeOfType(typeof(Selection));
            result.Should().BeEquivalentTo(new[] { 1, 2 });
        }

        [TestMethod]
        public void SelectionPropertyResolution_CorrectSetup_Success()
        {
            var r = new Resolver();
            var array = new[]
            {
                new { P1 = "1" },
                new { P1 = "2" }
            };
            var path = "[].P1";

            var result = r.Resolve(array, path) as IEnumerable;
            result.Should().NotBeNull();
            result.Should().BeEquivalentTo(new[] { "1", "2" });
        }

        [TestMethod]
        public void SelectionDictionaryKeyResolution_CorrectSetup_Success()
        {
            var r = new Resolver();
            var array = new[]
            {
                new Dictionary<string, string> { { "Key", "1" } },
                new Dictionary<string, string> { { "Key", "2" } }
            };
            var path = "[][Key]";

            var result = r.Resolve(array, path) as IEnumerable;
            result.Should().NotBeNull();
            result.Should().BeEquivalentTo(new[] { "1", "2" });
        }

        [TestMethod]
        public void SelectionArrayIndexResolution_CorrectSetup_Success()
        {
            var r = new Resolver();
            var array = new[]
            {
                new[] { "1", "2" },
                new[] { "3", "4" }
            };
            var path = "[][1]";

            var result = r.Resolve(array, path) as IEnumerable;
            result.Should().NotBeNull();
            result.Should().BeEquivalentTo(new[] { "2", "4" });
        }

        [TestMethod]
        public void SelectionFlattening_CorrectSetup_Success()
        {
            var r = new Resolver();
            var array = new object[]
            {
                1,
                new[] { 2, 3 }
            };
            var path = "[][]";

            var result = r.Resolve(array, path) as IEnumerable;
            result.Should().NotBeNull();
            result.Should().BeEquivalentTo(new[] { 1, 2, 3 });
        }

        [TestMethod]
        public void SinglePropertyResolution_NoPathElementTypeForPath_FailWithNoApplicablePathElementType()
        {
            var r = new Resolver();
            var o = new { Property = "1" };
            var path = "PropertyInfo^%#";

            r.Invoking(re => re.Resolve(o, path)).Should().Throw<InvalidOperationException>();
        }

        [TestMethod]
        public void SinglePropertyResolution_NonExistingProperty_FailWithPropertyCouldNotBeFound()
        {
            var r = new Resolver();
            var o = new { Property = "1" };
            var path = "NonExistingProperty";

            r.Invoking(re => re.Resolve(o, path)).Should().Throw<ArgumentException>();
        }

        [TestMethod]
        public void ArrayIndexResolution_IndexHigher_FailWithIndexTooHigh()
        {
            var r = new Resolver();
            var array = new[] { "1", "2" };
            var path = "[3]";

            r.Invoking(re => re.Resolve(array, path)).Should().Throw<IndexOutOfRangeException>();
        }

        [TestMethod]
        public void ArrayIndexResolution_IndexLower_FailWithNoApplicablePathElementType()
        {
            var r = new Resolver();
            var array = new[] { "1", "2" };
            var path = "[-2]";

            r.Invoking(re => re.Resolve(array, path)).Should().Throw<InvalidOperationException>();
        }

        [TestMethod]
        public void DictionaryKeyResolution_KeyNotExisting_FailWithKeyNotExisting()
        {
            var r = new Resolver();
            var dictionary = new Dictionary<string, string> { { "Key", "Value" } };
            var path = "[NonExistingKey]";

            r.Invoking(re =>
            {
                var resolve = re.Resolve(dictionary, path);
            }).Should().Throw<ArgumentException>();
        }

        [TestMethod]
        public void IndexerResolution_Int_Success()
        {
            var target = new IntIndexerClassNoIEnumerable("Test");
            var resolver = new Resolver();
            var path = "[123456]";

            var res = resolver.Resolve(target, path);
            res.Should().Be("Test123456");
        }

        [TestMethod]
        public void IndexerResolution_String_Success()
        {
            var target = new StringIndexerClassNoIEnumerable("Test");
            var resolver = new Resolver();
            var path = "[abc]";

            var res = resolver.Resolve(target, path);
            res.Should().Be("Testabc");
        }

        [TestMethod]
        public void SetValue_string_enum()
        {
            var r = new Resolver();
            var complex = new
            {
                Name = "Base Name",
                Programs = new Program[]
                {
                    new Program()
                    {
                        Name = "Prog1",
                        Products = new Product[]
                        {
                            new Product()
                            {
                                Name = "Prod1",
                                RepaymentPlan = new RepaymentPlan
                                {
                                    Terms = new TermOption[]
                                    {
                                        new TermOption { Min = 5, Max = 60 },
                                        new TermOption { Min = 12, Max = 120 }
                                    },
                                    FrequencyOptions = new List<FrequencyOption>()
                                    {
                                        new FrequencyOption { Frequency = RepaymentFrequency.Monthly },
                                        new FrequencyOption { Frequency = RepaymentFrequency.Quarterly },
                                    }
                                }
                            }
                        },
                        Fees = new Fee[]
                        {
                            new Fee() { Amount = 1.5M }
                        }
                    },
                }
            };

            var path = "Programs[0].Products[0].RepaymentPlan.FrequencyOptions[1].Frequency";

            var result = r.Resolve(complex, path);
            result.Should().NotBeNull();
            ((Property)result).SetValue("Weekly");
            complex.Programs[0].Products[0].RepaymentPlan.FrequencyOptions[1].Frequency.Should().Be(RepaymentFrequency.Weekly);
        }

        [TestMethod]
        public void SetValue_string_DateTime()
        {
            var r = new Resolver();
            var complex = new
            {
                Name = "Base Name",
                Programs = new Program[]
                {
                    new Program() { Name = "Prog1", Date = new DateTime(2022,1,1) }
                }
            };

            var path = "Programs[0].Date";

            var result = r.Resolve(complex, path);
            var expected = new DateTime(2020,1,30);
            ((Property)result).SetValue(expected.ToShortDateString());
            complex.Programs[0].Date.Should().Be(expected);
        }

        [TestMethod]
        public void SetValue_string_int()
        {
            var r = new Resolver();
            var complex = new
            {
                Name = "Base Name",
                Programs = new Program[]
                {
                    new Program() { Name = "Prog1", Number = 2 }
                }
            };

            var path = "Programs[0].Number";

            var result = r.Resolve(complex, path);
            ((Property)result).SetValue("5");
            complex.Programs[0].Number.Should().Be(5);
        }

        [TestMethod]
        public void SetValue_string_decimal()
        {
            var r = new Resolver();
            var complex = new
            {
                Name = "Base Name",
                Programs = new Program[]
                {
                    new Program() { Name = "Prog1", Money = 2.5M },
                    new Program() { Name = "Prog2", Money = 3.5M },
                }
            };

            var path = "Programs[0].Money";

            var result = r.Resolve(complex, path);
            ((Property)result).SetValue("25.0");
            complex.Programs[0].Money.Should().Be(25.0M);
        }

        public class TermOption
        {
            public int Min { get; set; }
            public int Max { get; set; }
        }

        public enum RepaymentFrequency
        {
            Weekly,
            Monthly,
            Quarterly,
            Annually
        }

        public class FrequencyOption
        {
            public RepaymentFrequency Frequency { get; set; }
        }

        public class RepaymentPlan
        {
            public TermOption[] Terms { get; set; }
            public List<FrequencyOption> FrequencyOptions { get; set; }
        }

        public class Fee
        {
            public decimal Amount { get; set; }
        }


        public class Product
        {
            public string Name { get; set; }
            public RepaymentPlan RepaymentPlan { get; set; }
        }

        public class Program
        {
            public Product[] Products { get; set; }
            public string Name { get; set; }
            public decimal Money { get; set; }
            public int Number { get; set; }
            public DateTime Date { get; set; }
            public Fee[] Fees { get; set; }
        }
    }
}
