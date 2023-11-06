using System.Collections.Concurrent;
using System.Diagnostics;

namespace MSTestX.UnitTestRunner
{
    internal static class TestResultSerializer
    {
        public static void Serialize(Stream s, TestResult t)
        {
            BinaryWriter bw = new BinaryWriter(s);
            var fields = TestResultFields.Select(f => (f.Name, f.GetValue(t))).Where(f => f.Item2 != null).ToArray();
            int fieldCount = fields.Count();
            if (t.Messages.Count == 0) fieldCount--;
            if (t.Attachments.Count == 0) fieldCount--;
            bw.Write(fieldCount);
            foreach (var field in fields)
            {
                if (field.Name == nameof(TestResult.Messages))
                {
                    if (t.Messages.Count > 0)
                    {
                        bw.Write(field.Name);
                        bw.Write(t.Messages.Count);
                        foreach (var m in t.Messages)
                        {
                            bw.Write(m.Category);
                            bw.Write(m.Text);
                        }
                    }
                    continue;
                }
                if (field.Name == nameof(TestResult.Attachments) && t.Attachments.Count == 0)
                    continue;
                
                bw.Write(field.Name);
                WriteType(bw, field.Item2);
            }
        }

        private static void WriteType(BinaryWriter bw, object value)
        {
            if(value is int i)
            {
                bw.Write(i);
            }
            else if (value is short s)
            {
                bw.Write(s);
            }
            else if (value is string str)
            {
                bw.Write(str);
            }
            else if (value is Enum)
            {
                bw.Write((int)value);
            }
            else if (value is DateTime dt)
            {
                bw.Write(dt.ToBinary());
            }
            else if (value is DateTimeOffset dto)
            {
                bw.Write(dto.ToUnixTimeMilliseconds());
            }
            else if (value is TimeSpan t)
            {
                bw.Write(t.Ticks);
            }
            else if (value is AttachmentSet attachmentSet) {
                bw.Write(attachmentSet.Uri.OriginalString);
                bw.Write(attachmentSet.DisplayName);
                WriteType(bw, attachmentSet.Attachments);
            }
            else if (value is UriDataAttachment attachment)
            {
                bw.Write(attachment.Uri.OriginalString);
                bw.Write(attachment.Description);
            }
            else if(value is System.Collections.IEnumerable enumerable)
            {
                var enumerator = enumerable.GetEnumerator();
                List<object> data = new List<object>();
                while (enumerator.MoveNext())
                    data.Add(enumerator.Current);
                bw.Write(data.Count);
                foreach(var item in data)
                {
                    WriteType(bw, item);
                }
            }
            else if(value.GetType().IsValueType)
            {
                throw new NotImplementedException(value.GetType().FullName);
            }
            else if(value is object)
            {
                bw.Write(value.GetType().AssemblyQualifiedName);
                var fields = value.GetType().GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance).Where(f=>f.GetSetMethod() != null).Where(f => f.GetValue(value) != null  || f.PropertyType.IsAssignableFrom(typeof(System.Collections.ObjectModel.Collection<>)));
                bw.Write(fields.Count());
                foreach (var field in fields)
                {
                    try
                    {
                        var value2 = field.GetValue(value);
                        bw.Write(field.Name);
                        WriteType(bw, value2);
                    }
                    catch (System.Exception ex)
                    {

                    }
                }
            }
            else
            {
                throw new NotImplementedException(value.GetType().FullName);
            }
        }

        static System.Reflection.PropertyInfo[] TestResultFields = typeof(TestResult).GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance).Where(f => f.Name != "TestCase" && f.GetSetMethod(true) != null).ToArray();
        public static TestResult Deserialize(byte[] data, TestCase testCase)
        {
            using var ms = new MemoryStream(data);
            var  br = new BinaryReader(ms);
#if !NETSTANDARD2_0
            TestResult result = new TestResult(testCase);
            
            var fieldCount = br.ReadInt32();
            for (int i = 0; i < fieldCount; i++)
            {
                var fieldName = br.ReadString();
                switch(fieldName)
                {
                    case nameof(TestResult.ComputerName):
                        result.ComputerName = br.ReadString(); break;
                    case nameof(TestResult.DisplayName):
                        result.DisplayName = br.ReadString(); break;
                    case nameof(TestResult.Duration):
                        result.Duration = TimeSpan.FromTicks(br.ReadInt64()); break;
                    case nameof(TestResult.EndTime):
                        result.EndTime = DateTimeOffset.FromUnixTimeMilliseconds(br.ReadInt64()); break;
                    case nameof(TestResult.ErrorMessage):
                        result.ErrorMessage = br.ReadString(); break;
                    case nameof(TestResult.ErrorStackTrace):
                        result.ErrorStackTrace = br.ReadString(); break;
                    case nameof(TestResult.Outcome):
                        result.Outcome = (TestOutcome)br.ReadInt32(); break;
                    case nameof(TestResult.StartTime):
                        result.StartTime = DateTimeOffset.FromUnixTimeMilliseconds(br.ReadInt64()); break;
                    case nameof(TestResult.Messages):
                        int count = br.ReadInt32();
                        for (int m = 0; m < count; m++)
                        {
                            result.Messages.Add(new TestResultMessage(br.ReadString(), br.ReadString()));
                        }
                        break;
                    // case nameof(TestResult.Attachments):
                    // case nameof(TestResult.Properties):
                    // case nameof(TestResult.Traits):
                    default:
                        var field = TestResultFields.Where(f => f.Name == fieldName).Single();
                        var value = ReadValue(br, field.PropertyType, field.GetValue(result));
                        if (value != null)
                            field.SetValue(result, value);
                        break;
                }
                //Debug.WriteLine(fieldName);
            }
            return result;
#else
            return null;
#endif
        }
        static ConcurrentDictionary<Type, System.Reflection.PropertyInfo[]> TypeInfo = new ConcurrentDictionary<Type, System.Reflection.PropertyInfo[]>();
        private static System.Reflection.PropertyInfo[] GetTypeInfo(Type t)
        {
            if(TypeInfo.ContainsKey(t))
                return TypeInfo[t];
            var fields = t.GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance).Where(f => f.GetSetMethod(true) != null).ToArray();
            TypeInfo[t] = fields;
            return fields;
        }
        private static object ReadValue(BinaryReader br, Type fieldType, object currentValue)
        {
            if (fieldType == typeof(int))
                return br.ReadInt32();
            if (fieldType == typeof(short))
                return br.ReadInt16();
            if (fieldType == typeof(string))
                return br.ReadString();
            if (fieldType == typeof(DateTime))
                return DateTime.FromBinary(br.ReadInt64());
            if (fieldType == typeof(DateTimeOffset))
                return DateTimeOffset.FromUnixTimeMilliseconds(br.ReadInt64());
            if (fieldType == typeof(TimeSpan))
                return TimeSpan.FromTicks(br.ReadInt64());
            if (fieldType.IsEnum)
                return br.ReadInt32();
            if (fieldType == typeof(AttachmentSet))
            {
                var uri = br.ReadString();
                Debug.WriteLine(uri);
                var set = new AttachmentSet(new Uri(uri), br.ReadString());
                ReadValue(br, set.Attachments.GetType(), set.Attachments);
                return set;
            }
            if (fieldType == typeof(UriDataAttachment))
                return new UriDataAttachment(new Uri(br.ReadString()), br.ReadString());
            if (fieldType.GetInterface("System.Collections.IEnumerable") != null)
            {
                int count = br.ReadInt32();
                object value = null;
                if (currentValue is null)
                {
                    value = Activator.CreateInstance(fieldType);
                    currentValue = value;
                }
                if (currentValue is System.Collections.IList list)
                {
                    Type elementType = null;
                    if (fieldType.IsGenericType)
                        elementType = fieldType.GenericTypeArguments[0];
                    else
                    {
                        //TODO but not really getting hit so :shrug:
                    }
                    for (int i = 0; i < count; i++)
                    {
                        var itemValue = ReadValue(br, elementType, null);
                        list.Add(itemValue);
                    }
                }
                return value;
            }
            if (!fieldType.IsValueType)
            {
                var typeName = br.ReadString();
                int fieldCount = br.ReadInt32();

#if !NETSTANDARD2_0
                var instance = currentValue ?? System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(Type.GetType(typeName));
                var fields = GetTypeInfo(fieldType);
                for (int i = 0; i < fieldCount; i++) {
                    var fieldName = br.ReadString();
                    var field = fields.Where(f => f.Name == fieldName).Single();
                    var value = ReadValue(br, field.PropertyType, field.GetValue(instance));
                    field.GetSetMethod(true).Invoke(instance, new object[] { value });
                }
                return instance;
#endif
            }
            throw new NotImplementedException(fieldType.FullName);
        }
    }
}
