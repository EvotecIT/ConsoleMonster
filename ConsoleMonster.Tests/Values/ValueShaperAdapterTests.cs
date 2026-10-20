using System.Collections.Generic;
using ConsoleMonster.Data;
using ConsoleMonster.Values;
using Xunit;

namespace ConsoleMonster.Tests.Values {
    public sealed class ValueShaperAdapterTests {
        private sealed class Record {
            public int Reads;
            public string Value { get { Reads++; return "reflected"; } }
        }

        private sealed class Adapter : IMemberSource {
            public bool CanRead(object value) => value is Record;
            public string GetTypeName(object value) => "Record";
            public IEnumerable<InspectMember> GetMembers(object value) {
                yield return new InspectMember("Value", () => "supplied");
            }
        }

        [Fact]
        public void AdapterOnlyMode_DoesNotInvokePocoGetters_AndReadsAnExplicitAdapter() {
            Record record = new Record();
            ShapeOptions options = new ShapeOptions { UseReflection = false };
            Assert.Empty(ValueShaper.Fields(record, options));
            Assert.Equal(0, record.Reads);
            options = new ShapeOptions { UseReflection = false };
            options.Sources.Add(new Adapter());
            InspectMember member = Assert.Single(ValueShaper.Fields(record, options));
            Assert.Equal("supplied", ValueShaper.Read(member));
            Assert.Equal(0, record.Reads);
        }
    }
}
