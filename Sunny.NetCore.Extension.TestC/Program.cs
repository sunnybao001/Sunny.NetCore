using Sunny.NetCore.Extension.Converter;
using System;
using System.Diagnostics;

namespace Sunny.NetCore.Extension.TestC
{
	class Program
	{
		static void Main(string[] args)
		{
			Console.WriteLine("Hello World!");

			TestLong();
			//TestGUID();
			TestDateTime();
			TestInt();
		}
		public static void TestGUID()
		{
			var guid = Guid.NewGuid();
			var str = GuidInterface.Singleton.GuidToString(ref guid);
			GuidInterface.Singleton.TryParseGuid(str, out var ng);
			//Assert.AreEqual(guid, ng);
			System.Text.Json.JsonSerializerOptions jsonOptions = new System.Text.Json.JsonSerializerOptions
			{
				Converters = { GuidInterface.Singleton }
			};
			str = System.Text.Json.JsonSerializer.Serialize(guid, jsonOptions);
			ng = System.Text.Json.JsonSerializer.Deserialize<Guid>(str, jsonOptions);

			var sw = Stopwatch.StartNew();
			for (var i = 0; i < 1000000; ++i)
			{
				guid = Guid.Parse(guid.ToString());
			}
			sw.Stop();
			Console.WriteLine("Guid自带转换耗时：" + sw.ElapsedMilliseconds.ToString());
			sw.Restart();
			for (var i = 0; i < 1000000; ++i)
			{
				if (!GuidInterface.Singleton.TryParseGuid(GuidInterface.Singleton.GuidToString(ref guid), out guid)) throw new Exception();
			}
			sw.Stop();
			Console.WriteLine("Sunny库转换耗时：" + sw.ElapsedMilliseconds.ToString());
			Console.ReadLine();
		}
		public static void TestDateTime()
		{
			var dt = DateTime.Today;
			var str = DateFormat.Singleton.DateTimeToString(dt);
			DateFormat.Singleton.TryParseDateTime(str, out var ndt);
			//Assert.AreEqual(dt, ndt);

			dt = DateTime.UtcNow;
			dt = new DateTime(dt.Ticks - (dt.Ticks % TimeSpan.TicksPerSecond));
			str = DateFormat.Singleton.DateTimeToString(dt);
			DateFormat.Singleton.TryParseDateTime(str, out ndt);
			//Assert.AreEqual(dt, ndt);

			DateFormat.Singleton.TryParseDateTime("2020-8-8", out dt);
			//Assert.AreEqual(dt, new DateTime(2020, 8, 8));

			dt = DateTime.UtcNow;
			var sw = Stopwatch.StartNew();
			for (var i = 0; i < 1000000; ++i)
			{
				dt = DateTime.Parse(dt.ToString());
			}
			sw.Stop();
			Console.WriteLine("DateTime自带转换耗时：" + sw.ElapsedMilliseconds.ToString());
			sw.Restart();
			for (var i = 0; i < 1000000; ++i)
			{
				if (!DateFormat.Singleton.TryParseDateTime(DateFormat.Singleton.DateTimeToString(dt), out dt)) throw new Exception();
			}
			sw.Stop();
			Console.WriteLine("Sunny库转换耗时：" + sw.ElapsedMilliseconds.ToString());
			Console.ReadLine();
		}
		public static void TestInt()
		{
			var i = Guid.NewGuid().GetHashCode();
			var str = IntInterface.Singleton.IntToString(i);
			IntInterface.Singleton.TryParseInt(str, out var ni);
			//Assert.AreEqual(i, ni);
		}
		public static void TestLong()
		{
			var l = Generator.LongValueGenerator.NextValue();
			var str = LongInterface.Singleton.LongToString(l);
			LongInterface.Singleton.TryParseLong(str, out var nl);
			//Assert.AreEqual(l, nl);
		}
	}
}
