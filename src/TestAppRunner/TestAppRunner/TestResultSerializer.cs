namespace MSTestX.UnitTestRunner
{
    internal static class TestResultSerializer
    {
        public static void Serialize(Stream s, TestResult t)
        {
            BinaryWriter bw = new BinaryWriter(s);
            var fields = t.GetType().GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance).Where(f=>f.Name != "TestCase" && f.GetValue(t) != null && f.GetSetMethod(true) != null);
            bw.Write(fields.Count());
            foreach (var field in fields)
            {
                var value = field.GetValue(t);
                bw.Write(field.Name);
                WriteType(bw, value);
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
                var fields = value.GetType().GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance).Where(f=>f.GetSetMethod(true) != null).Where(f => f.GetValue(value) != null);
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

        public static TestResult Deserialize(byte[] data, TestCase testCase)
        {
            using var ms = new MemoryStream(data);
            var  br = new BinaryReader(ms);
#if !NETSTANDARD2_0
            TestResult result = new TestResult(testCase);
            var fields = result.GetType().GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            var fieldCount = br.ReadInt32();
            for (int i = 0; i < fieldCount; i++)
            {
                var fieldName = br.ReadString();
                var field = fields.Where(f => f.Name == fieldName).Single();
                var value = ReadValue(br, field.PropertyType, field.GetValue(result));
                if (value != null)
                    field.SetValue(result, value);
            }
            return result;
#else
            return null;
#endif
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
                var fields = fieldType.GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
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
