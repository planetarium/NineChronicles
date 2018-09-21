using System;
using UnityEngine;

namespace Anima2D 
{
	public static class MathUtils
	{
		public static float SignedAngle(Vector3 a, Vector3 b, Vector3 forward)
		{
			float angle = Vector3.Angle (a, b);
			float sign = Mathf.Sign (Vector3.Dot (forward, Vector3.Cross (a, b)));
			
			return angle * sign;
		}

		public static float Fmod(float value, float mod)
		{
			return Mathf.Abs(value % mod + mod) % mod;
		}

		public static float SegmentDistance(Vector3 point, Vector3 a, Vector3 b)
		{
			Vector2 v = b - a;
			Vector2 p = point - a;
			
			float dot = Vector2.Dot(v,p);
			
			if(dot <= 0f)
			{
				return p.magnitude;
			}
			
			if(dot >= v.sqrMagnitude)
			{
				return (point - b).magnitude;
			}
			
			return LineDistance(point,a,b);
		}

		public static float SegmentSqrtDistance(Vector3 point, Vector3 a, Vector3 b)
		{
			Vector2 v = b - a;
			Vector2 p = point - a;
			
			float dot = Vector2.Dot(v,p);
			
			if(dot <= 0f)
			{
				return p.sqrMagnitude;
			}
			
			if(dot >= v.sqrMagnitude)
			{
				return (point - b).sqrMagnitude;
			}
			
			return SqrtLineDistance(point,a,b);
		}
		
		public static float LineDistance(Vector3 point, Vector3 a, Vector3 b)
		{
			return Mathf.Sqrt(SqrtLineDistance(point,a,b));
		}
		
		public static float SqrtLineDistance(Vector3 point, Vector3 a, Vector3 b)
		{
			float num = Mathf.Abs((b.y-a.y)*point.x - (b.x-a.x)*point.y + b.x*a.y - b.y*a.x);
			return num*num / ((b.y - a.y)*(b.y - a.y) + (b.x-a.x)*(b.x-a.x));
		}

		public static void WorldFromMatrix4x4(this Transform transform, Matrix4x4 matrix)
		{
			transform.localScale = matrix.GetScale();
			transform.rotation = matrix.GetRotation();
			transform.position = matrix.GetPosition();
		}

		public static void LocalFromMatrix4x4(this Transform transform, Matrix4x4 matrix)
		{
			transform.localScale = matrix.GetScale();
			transform.localRotation = matrix.GetRotation();
			transform.localPosition = matrix.GetPosition();
		}
		
		public static Quaternion GetRotation(this Matrix4x4 matrix)
		{
			var qw = Mathf.Sqrt(1f + matrix.m00 + matrix.m11 + matrix.m22) / 2;
			var w = 4 * qw;
			var qx = (matrix.m21 - matrix.m12) / w;
			var qy = (matrix.m02 - matrix.m20) / w;
			var qz = (matrix.m10 - matrix.m01) / w;
			
			return new Quaternion(qx, qy, qz, qw);
		}
		
		public static Vector3 GetPosition(this Matrix4x4 matrix)
		{
			var x = matrix.m03;
			var y = matrix.m13;
			var z = matrix.m23;
			
			return new Vector3(x, y, z);
		}
		
		public static Vector3 GetScale(this Matrix4x4 m)
		{
			var x = Mathf.Sqrt(m.m00 * m.m00 + m.m01 * m.m01 + m.m02 * m.m02);
			var y = Mathf.Sqrt(m.m10 * m.m10 + m.m11 * m.m11 + m.m12 * m.m12);
			var z = Mathf.Sqrt(m.m20 * m.m20 + m.m21 * m.m21 + m.m22 * m.m22);
			
			return new Vector3(x, y, z);
		}

		public static Rect ClampRect(Rect rect, Rect frame)
		{
			return new Rect {
				xMin = Mathf.Clamp(rect.xMin, 0f, (float)(frame.width - 1)),
				yMin = Mathf.Clamp(rect.yMin, 0f, (float)(frame.height - 1)),
				xMax = Mathf.Clamp(rect.xMax, 1f, (float) frame.width),
				yMax = Mathf.Clamp(rect.yMax, 1f, (float) frame.height)
			};
		}

		public static Vector2 ClampPositionInRect(Vector2 position, Rect frame)
		{
			return new Vector2(Mathf.Clamp(position.x,frame.xMin,frame.xMax), Mathf.Clamp(position.y,frame.yMin,frame.yMax));
		}

		public static Rect OrderMinMax(Rect rect)
		{
			if (rect.xMin > rect.xMax)
			{
				float xMin = rect.xMin;
				rect.xMin = rect.xMax;
				rect.xMax = xMin;
			}
			if (rect.yMin > rect.yMax)
			{
				float yMin = rect.yMin;
				rect.yMin = rect.yMax;
				rect.yMax = yMin;
			}
			return rect;
		}

		public static float RoundToMultipleOf(float value, float roundingValue)
		{
			if (roundingValue == 0f)
			{
				return value;
			}
			return Mathf.Round(value / roundingValue) * roundingValue;
		}

		public static float GetClosestPowerOfTen(float positiveNumber)
		{
			if (positiveNumber <= 0f)
			{
				return 1f;
			}
			return Mathf.Pow(10f, (float)Mathf.RoundToInt(Mathf.Log10(positiveNumber)));
		}

		public static float RoundBasedOnMinimumDifference(float valueToRound, float minDifference)
		{
			if (minDifference == 0f)
			{
				return MathUtils.DiscardLeastSignificantDecimal(valueToRound);
			}
			return (float)Math.Round((double)valueToRound, MathUtils.GetNumberOfDecimalsForMinimumDifference(minDifference), MidpointRounding.AwayFromZero);
		}

		public static float DiscardLeastSignificantDecimal(float v)
		{
			int digits = Mathf.Clamp((int)(5f - Mathf.Log10(Mathf.Abs(v))), 0, 15);
			return (float)Math.Round((double)v, digits, MidpointRounding.AwayFromZero);
		}

		public static int GetNumberOfDecimalsForMinimumDifference(float minDifference)
		{
			return Mathf.Clamp(-Mathf.FloorToInt(Mathf.Log10(minDifference)), 0, 15);
		}

		public static Vector3 Unskin(Vector3 skinedPosition,
			Matrix4x4 localToWorld,
			UnityEngine.BoneWeight boneWeight,
			Matrix4x4[] bindposes,
			Transform[] bones)
		{
			Matrix4x4 m0 = bones[boneWeight.boneIndex0].localToWorldMatrix * bindposes[boneWeight.boneIndex0];
			Matrix4x4 m1 = bones[boneWeight.boneIndex1].localToWorldMatrix * bindposes[boneWeight.boneIndex1];
			Matrix4x4 m2 = bones[boneWeight.boneIndex2].localToWorldMatrix * bindposes[boneWeight.boneIndex2];
			Matrix4x4 m3 = bones[boneWeight.boneIndex3].localToWorldMatrix * bindposes[boneWeight.boneIndex3];

			Matrix4x4 m = Matrix4x4.identity;

			for(int n=0;n<16;n++)
			{
				m0[n] *= boneWeight.weight0;
				m1[n] *= boneWeight.weight1;
				m2[n] *= boneWeight.weight2;
				m3[n] *= boneWeight.weight3;
				m[n] = m0[n]+m1[n]+m2[n]+m3[n];
			}

			return localToWorld.MultiplyPoint3x4( m.inverse.MultiplyPoint3x4(skinedPosition) );
		}
	}
}
