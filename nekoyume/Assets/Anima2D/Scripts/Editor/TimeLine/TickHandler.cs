using System;
using System.Collections.Generic;
using UnityEngine;

namespace Anima2D
{
	[Serializable]
	internal class TickHandler
	{
		private float[] m_TickModulos = new float[0];
		private float[] m_TickStrengths = new float[0];
		private int m_SmallestTick;
		private int m_BiggestTick = -1;
		private float m_MinValue;
		private float m_MaxValue = 1f;
		private float m_PixelRange = 1f;
		public int tickLevels
		{
			get
			{
				return this.m_BiggestTick - this.m_SmallestTick + 1;
			}
		}
		public void SetTickModulos(float[] tickModulos)
		{
			this.m_TickModulos = tickModulos;
		}
		public void SetTickModulosForFrameRate(float frameRate)
		{
			if (frameRate != Mathf.Round(frameRate))
			{
				this.SetTickModulos(new float[]
					{
						1f / frameRate,
						5f / frameRate,
						10f / frameRate,
						50f / frameRate,
						100f / frameRate,
						500f / frameRate,
						1000f / frameRate,
						5000f / frameRate,
						10000f / frameRate,
						50000f / frameRate,
						100000f / frameRate,
						500000f / frameRate
					});
			}
			else
			{
				List<int> list = new List<int>();
				int num = 1;
				while ((float)num < frameRate)
				{
					if ((float)num == frameRate)
					{
						break;
					}
					int num2 = Mathf.RoundToInt(frameRate / (float)num);
					if (num2 % 60 == 0)
					{
						num *= 2;
						list.Add(num);
					}
					else
					{
						if (num2 % 30 == 0)
						{
							num *= 3;
							list.Add(num);
						}
						else
						{
							if (num2 % 20 == 0)
							{
								num *= 2;
								list.Add(num);
							}
							else
							{
								if (num2 % 10 == 0)
								{
									num *= 2;
									list.Add(num);
								}
								else
								{
									if (num2 % 5 == 0)
									{
										num *= 5;
										list.Add(num);
									}
									else
									{
										if (num2 % 2 == 0)
										{
											num *= 2;
											list.Add(num);
										}
										else
										{
											if (num2 % 3 == 0)
											{
												num *= 3;
												list.Add(num);
											}
											else
											{
												num = Mathf.RoundToInt(frameRate);
											}
										}
									}
								}
							}
						}
					}
				}
				float[] array = new float[9 + list.Count];
				for (int i = 0; i < list.Count; i++)
				{
					array[i] = 1f / (float)list[list.Count - i - 1];
				}
				array[array.Length - 1] = 3600f;
				array[array.Length - 2] = 1800f;
				array[array.Length - 3] = 600f;
				array[array.Length - 4] = 300f;
				array[array.Length - 5] = 60f;
				array[array.Length - 6] = 30f;
				array[array.Length - 7] = 10f;
				array[array.Length - 8] = 5f;
				array[array.Length - 9] = 1f;
				this.SetTickModulos(array);
			}
		}
		public void SetRanges(float minValue, float maxValue, float minPixel, float maxPixel)
		{
			this.m_MinValue = minValue;
			this.m_MaxValue = maxValue;
			this.m_PixelRange = maxPixel - minPixel;
		}
		public float[] GetTicksAtLevel(int level, bool excludeTicksFromHigherlevels)
		{
			int num = Mathf.Clamp(this.m_SmallestTick + level, 0, this.m_TickModulos.Length - 1);
			List<float> list = new List<float>();
			int num2 = Mathf.FloorToInt(this.m_MinValue / this.m_TickModulos[num]);
			int num3 = Mathf.CeilToInt(this.m_MaxValue / this.m_TickModulos[num]);
			for (int i = num2; i <= num3; i++)
			{
				if (!excludeTicksFromHigherlevels || num >= this.m_BiggestTick || i % Mathf.RoundToInt(this.m_TickModulos[num + 1] / this.m_TickModulos[num]) != 0)
				{
					list.Add((float)i * this.m_TickModulos[num]);
				}
			}
			return list.ToArray();
		}
		public float GetStrengthOfLevel(int level)
		{
			return this.m_TickStrengths[this.m_SmallestTick + level];
		}
		public float GetPeriodOfLevel(int level)
		{
			return this.m_TickModulos[Mathf.Clamp(this.m_SmallestTick + level, 0, this.m_TickModulos.Length - 1)];
		}
		public int GetLevelWithMinSeparation(float pixelSeparation)
		{
			for (int i = 0; i < this.m_TickModulos.Length; i++)
			{
				float num = this.m_TickModulos[i] * this.m_PixelRange / (this.m_MaxValue - this.m_MinValue);
				if (num >= pixelSeparation)
				{
					return i - this.m_SmallestTick;
				}
			}
			return -1;
		}
		public void SetTickStrengths(float tickMinSpacing, float tickMaxSpacing, bool sqrt)
		{
			this.m_TickStrengths = new float[this.m_TickModulos.Length];
			this.m_SmallestTick = 0;
			this.m_BiggestTick = this.m_TickModulos.Length - 1;
			for (int i = this.m_TickModulos.Length - 1; i >= 0; i--)
			{
				float num = this.m_TickModulos[i] * this.m_PixelRange / (this.m_MaxValue - this.m_MinValue);
				this.m_TickStrengths[i] = (num - tickMinSpacing) / (tickMaxSpacing - tickMinSpacing);
				if (this.m_TickStrengths[i] >= 1f)
				{
					this.m_BiggestTick = i;
				}
				if (num <= tickMinSpacing)
				{
					this.m_SmallestTick = i;
					break;
				}
			}
			for (int j = this.m_SmallestTick; j <= this.m_BiggestTick; j++)
			{
				this.m_TickStrengths[j] = Mathf.Clamp01(this.m_TickStrengths[j]);
				if (sqrt)
				{
					this.m_TickStrengths[j] = Mathf.Sqrt(this.m_TickStrengths[j]);
				}
			}
		}
	}
}