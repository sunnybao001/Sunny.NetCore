using Microsoft.EntityFrameworkCore.ChangeTracking;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using System.Text.Json;
using System.Threading.Tasks;

namespace Sunny.NetCore.Extension.Generator
{
	public class LongValueGenerator : Microsoft.EntityFrameworkCore.ValueGeneration.ValueGenerator
	{
		public override bool GeneratesTemporaryValues => false;

		protected override object NextValue(EntityEntry entry) => NextValue();
		public static long NextValue() => GetId(DateTime.UtcNow.Ticks);
		public static long OldToNewId(long old)
		{
			var dt = OldIdToDateTime(old);
			return GetId(dt.Ticks);
		}
		public static long GetMinValue(DateTime dt) => GetPrefix(dt.Ticks);
		public static long GetMaxValue(DateTime dt) => GetPrefix(dt.Ticks) + 0x1000000;
		private unsafe static DateTime OldIdToDateTime(long old)
		{
			int* f = (int*)&old;
			var ticks = (long)f[1];
			ticks <<= 24;
			ticks += new DateTime(2010, 1, 1).Ticks;
			return new DateTime(ticks, DateTimeKind.Utc);
		}
		private unsafe static long GetId(long ticks)
		{
			int r;
			do
			{
				r = Guid.NewGuid().GetHashCode();
			} while (r == default);
			//DateTime的Ticks前两位永远是0，但是第一位是符号位不可用
			long id = ticks << 1;
			byte* f = (byte*)&id;   //随机数占用24位，剩余40位，前1位舍去，密度为DateTime的41位，大约半秒多改变一次值
			byte* rf = (byte*)&r;
			f[0] = rf[0];
			f[1] = rf[1];
			f[2] = rf[2];
			return id;
		}
		private unsafe static long GetPrefix(long ticks)
		{
			long id = ticks << 1;
			byte* f = (byte*)&id;
			f[0] = f[1] = f[2] = 0;
			return id;
		}
	}
}
