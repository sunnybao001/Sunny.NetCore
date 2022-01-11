using Sunny.NetCore.Extension.Converter;
using System;
using System.Diagnostics;

namespace Sunny.NetCore.Extension.TestC
{
	class Program
	{
		static unsafe void Main(string[] args)
		{
			Console.WriteLine("Hello World!");


			TestLong();
			TestGUID();
			TestDateTime();
			TestDateOnly();
			TestHex();
			//TestInt();
			Console.ReadLine();
			Console.ReadLine();
		}
		public static void TestGUID()
		{
			var guid = Guid.NewGuid();
			var @interface = GuidInterface.Singleton;
			var str = @interface.GuidToString(ref guid);
			@interface.TryParse(str, out var ng);
			//Assert.AreEqual(guid, ng);
			System.Text.Json.JsonSerializerOptions jsonOptions = new System.Text.Json.JsonSerializerOptions
			{
				Converters = { @interface }
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
				if (!@interface.TryParse(@interface.GuidToString(ref guid), out guid)) throw new Exception();
			}
			sw.Stop();
			Console.WriteLine("Sunny库转换耗时：" + sw.ElapsedMilliseconds.ToString());
		}
		public unsafe static void TestDateTime()
		{
			var dt = DateTime.Today;
			var str = DateTimeFormat.Singleton.DateTimeToString(dt);
			DateTimeFormat.Singleton.TryParse(str, out var ndt);
			//Assert.AreEqual(dt, ndt);

			dt = DateTime.UtcNow;
			dt = new DateTime(dt.Ticks - (dt.Ticks % TimeSpan.TicksPerSecond));
			str = DateTimeFormat.Singleton.DateTimeToString(dt);
			DateTimeFormat.Singleton.TryParse(str, out ndt);
			//Assert.AreEqual(dt, ndt);

			DateTimeFormat.Singleton.TryParse("2020-8-8", out dt);
			//Assert.AreEqual(dt, new DateTime(2020, 8, 8));

			var str1 = stackalloc char[20];
			var span = new Span<char>(str1, 20);
			dt = DateTime.UtcNow;
			var sw = Stopwatch.StartNew();
			for (var i = 0; i < 1000000; ++i)
			{
				if (!dt.TryFormat(span, out var length)) throw new Exception();
				if (!DateTime.TryParse(span.Slice(0, length), out dt)) throw new Exception();
			}
			sw.Stop();
			Console.WriteLine("DateTime自带转换耗时：" + sw.ElapsedMilliseconds.ToString());
			sw.Restart();
			for (var i = 0; i < 1000000; ++i)
			{
				if (!DateTimeFormat.Singleton.TryFormat(dt, span, out var length)) throw new Exception();
				if (!DateTimeFormat.Singleton.TryParse(span.Slice(0, length), out dt)) throw new Exception();
			}
			sw.Stop();
			Console.WriteLine("Sunny库转换耗时：" + sw.ElapsedMilliseconds.ToString());
		}
		public unsafe static void TestDateOnly()
		{
			var str = stackalloc char[20];
			var span = new Span<char>(str, 20);
			var date = DateOnly.FromDateTime(DateTime.Today);
			var sw = Stopwatch.StartNew();
			for (var i = 0; i < 1000000; ++i)
			{
				if (!date.TryFormat(span, out var length)) throw new Exception();
				if (!DateOnly.TryParse(span.Slice(0, length), out date)) throw new Exception();
			}
			sw.Stop();
			Console.WriteLine("DateOnly自带转换耗时：" + sw.ElapsedMilliseconds.ToString());
			sw.Restart();
			for (var i = 0; i < 1000000; ++i)
			{
				if (!DateOnlyFormat.Singleton.TryFormat(date, span, out var length)) throw new Exception();
				if (!DateOnlyFormat.Singleton.TryParse(span.Slice(0, length), out date)) throw new Exception();
			}
			sw.Stop();
			Console.WriteLine("Sunny库转换耗时：" + sw.ElapsedMilliseconds.ToString());
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
			var @interface = LongInterface.Singleton;
			var str = @interface.LongToString(l);
			@interface.TryParse(str, out var nl);
			//Assert.AreEqual(l, nl);

			var sw = Stopwatch.StartNew();
			for (var i = 0; i < 1000000; ++i)
			{
				l = long.Parse(l.ToString());
			}
			sw.Stop();
			Console.WriteLine("long自带转换耗时：" + sw.ElapsedMilliseconds.ToString());
			sw.Restart();
			for (var i = 0; i < 1000000; ++i)
			{
				if (!@interface.TryParse(@interface.LongToString(l), out l)) throw new Exception();
			}
			sw.Stop();
			Console.WriteLine("Sunny库转换耗时：" + sw.ElapsedMilliseconds.ToString());
		}
		public static void TestHex()
		{
			var buffer = new byte[32];
			Random.Shared.NextBytes(buffer);
			var @interface = HexInterface.Singleton;
			var sw = Stopwatch.StartNew();
			for (var i = 0; i < 1000000; ++i)
			{
				Convert.ToHexString(buffer);
			}
			sw.Stop();
			Console.WriteLine("Convert自带转换耗时：" + sw.ElapsedMilliseconds.ToString());
			sw.Restart();
			for (var i = 0; i < 1000000; ++i)
			{
				@interface.BytesToString(buffer);
			}
			sw.Stop();
			Console.WriteLine("Sunny库转换耗时：" + sw.ElapsedMilliseconds.ToString());
		}
	}
}
