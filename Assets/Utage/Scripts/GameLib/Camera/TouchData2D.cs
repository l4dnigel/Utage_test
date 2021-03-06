//----------------------------------------------
// UTAGE: Unity Text Adventure Game Engine
// Copyright 2014 Ryohei Tokimura
//----------------------------------------------
using UnityEngine;
using System.Collections;
namespace Utage
{
	/// <summary>
	/// 2D用のタッチ入力データ
	/// </summary>
	[System.Serializable]
	public class TouchData2D
	{
		const string FuncTouchDown = "OnTouchDown";		//最初にタッチされたとき
		const string FuncTouch = "OnTouch";				//タッチされてる（コライダー内）
		const string FuncTouchOver = "OnTouchOver";		//タッチはしているが、コライダーから外れた
		const string FuncTouchUp = "OnTouchUp";			//タッチが離れた
		const string FuncClick = "OnClick";				//クリック（コライダー内でタッチが離れた）
		const string FuncDrag = "OnDrag";				//ドラッグ

		/// <summary>
		/// ドラッグ情報
		/// </summary>
		[System.Serializable]
		public struct DragLog
		{
			/// <summary>
			/// ドラッグすべきポイント
			/// </summary>
			public Vector2 point;

			/// <summary>
			/// 前フレームからの経過時間
			/// </summary>
			public float deltaTime;

			/// <summary>
			/// 方向
			/// </summary>
			public Vector2 direction;

			/// <summary>
			/// スピード
			/// </summary>
			public float speed;

			/// <summary>
			/// 方向を初期化
			/// </summary>
			/// <param name="lastPoint"></param>
			public void InitDirection(Vector2 lastPoint)
			{
				direction = point - lastPoint;
				speed = direction.magnitude / deltaTime;
				direction.Normalize();
			}

			/// <summary>
			/// 方向をクリア
			/// </summary>
			public void ClearDirection()
			{
				direction = Vector3.zero;
				speed = 0;
			}
		};

		/// <summary>
		/// 2D用の入力管理クラスへの参照
		/// </summary>
		public CameraInput2D Input { get { return input; } }
		CameraInput2D input;

		/// <summary>
		/// タッチ座標
		/// </summary>
		public Vector2 TouchPoint { get { return touchPoint; } }
		Vector2 touchPoint;

		/// <summary>
		/// 前フレームのタッチ座標
		/// </summary>
		public Vector2 LastPoint { get { return lastPoint; } }
		Vector2 lastPoint;

		/// <summary>
		/// 前フレームとの差分
		/// </summary>
		public Vector2 DeltaPoint { get { return TouchPoint - LastPoint; } }

		/// <summary>
		/// 開始時点のタッチ座標
		/// </summary>
		public Vector2 StartPoint { get { return startPoint; } }
		Vector2 startPoint;	//

		/// <summary>
		/// 最初のヒットしたときの、コライダーの位置と、タッチポイントの差分
		/// </summary>
		public Vector2 StartOffset { get { return startOffset; } }
		Vector2 startOffset;

		/// <summary>
		/// ドラッグ座標
		/// </summary>
		public Vector2 DragPoint { get { return dragPointLogs[0].point; } }

		/// <summary>
		/// ドラッグ座標のログ
		/// </summary>
		public DragLog[] DragPointLogs
		{
			get
			{
				if (dragPointNum == 0)
				{
					return dragPointLogs;
				}
				else
				{
					DragLog[] ret = new DragLog[dragPointNum];
					System.Array.Copy(dragPointLogs, ret, dragPointNum);
					return ret;
				}
			}
		}

		/// <summary>
		/// ドラッグする場合の方向（現在のフレームのみ）
		/// </summary>
		public Vector2 DragDirectionCurrent { get { return dragPointLogs[0].direction; } }

		/// <summary>
		/// ドラッグのスピード（現在のフレームのみ）
		/// </summary>
		public float DragSpeedCurrent { get { return dragPointLogs[0].speed; } }
		public float DragSpeedCurrentClamp(float max) { return Mathf.Min(max, DragSpeedCurrent); }

		/// <summary>
		/// ドラッグする場合の方向(今までの記録による平均化した値)
		/// </summary>
		public Vector2 DragDirection { get { return dragDirection; } }
		Vector2 dragDirection;

		/// <summary>
		/// ドラッグのスピード(今までの記録による平均化した値)
		/// </summary>
		public float DragSpeed { get { return dragSpeed; } }
		float dragSpeed;

		/// <summary>
		/// ドラッグのスピード(今までの記録による平均化した値)を最大値を指定して取得
		/// </summary>
		/// <param name="max">最大値</param>
		/// <returns></returns>
		public float DragSpeedClamp(float max) { return Mathf.Min(max, dragSpeed); }

		GameObject target;				//最初のコリジョンオブジェクト
		bool isOvered;					//最初のコリジョン範囲から外れた
		bool isCanceled;				//キャンセルされたか
		const int dragPointMax = 10;	//ドラッグを記録する最大数
		int dragPointNum;				//ドラッグを記録している数
		DragLog[] dragPointLogs = new DragLog[dragPointMax];	//ドラッグ座標のログ

		/// <summary>
		/// 入力を強制的にキャンセル（ドラッグ等が中断される）
		/// </summary>
		public void Cancel()
		{
			isCanceled = true;
			if (null != target)
			{
				Send(FuncTouchUp);
			}
		}

		/// <summary>
		/// コンストラクタ
		/// </summary>
		/// <param name="input">2D用の入力管理クラスへ</param>
		public TouchData2D(CameraInput2D input)
		{
			this.input = input;
		}

		/// <summary>
		/// タッチしているポイントの更新
		/// </summary>
		/// <param name="isPressed">押されているか</param>
		/// <param name="isTrig">押された瞬間か</param>
		/// <param name="x">座標X</param>
		/// <param name="y">座標Y</param>
		/// <param name="layerMask">レイヤー</param>
		public void UpdatePoint(bool isPressed, bool isTrig, float x, float y, int layerMask)
		{
			//キャンルされていた場合、タッチを離すまで処理が無効
			if (isCanceled)
			{
				if (!isPressed) EndTouch();
				return;
			}

			GameObject hit = CheckPoint(x, y, layerMask);
			//タッチ開始で初期化
			if (isTrig)
			{
				BeginTouch(x, y, hit);
			}

			//最初にタッチした先にコリジョンがなかった。処理終了
			if (target == null) return;

			//前フレームを記録
			lastPoint = touchPoint;
			//タッチ点を記録
			touchPoint.Set(x, y);

			if (isPressed)
			{
				//最初のタッチ処理
				if (isTrig) Send(FuncTouchDown);

				if (!isOvered)
				{
					if (target == hit)
					{
						//コリジョンから外れてない
						Send(FuncTouch);
					}
					else
					{
						//コリジョンから外れた
						isOvered = true;
						Send(FuncTouchOver);
					}
				}
				//ドラッグ操作
				AddDragLog(touchPoint);
				Send(FuncDrag);
			}
			else
			{
				//タッチを離した
				Send(FuncTouchUp);
				//ずっと最初のコリジョン内だった、クリック操作成立
				if (!isOvered) Send(FuncClick);
				EndTouch();
			}
		}

		//タッチ開始の初期化
		void BeginTouch(float x, float y, GameObject hit)
		{
			target = null;

			if (hit != null)
			{
				//タッチした先にコリジョンがある
				target = hit;
				isOvered = false;
				isCanceled = false;

				Vector2 point = new Vector2(x, y);

				touchPoint = lastPoint = startPoint = point;

				dragPointNum = 0;
				startOffset = hit.transform.position;
				startOffset -= startPoint;

				dragDirection = Vector2.zero;
				dragSpeed = 0;

			}
		}

		//タッチ終了
		void EndTouch()
		{
			target = null;
			isOvered = false;
			isCanceled = false;
		}

		//ドラッグによるログを記録
		void AddDragLog(Vector2 point)
		{
			const int dragPointMax = 10;
			dragPointNum = Mathf.Min(dragPointNum + 1, dragPointMax);
			for (int i = 0; i < dragPointNum - 1; ++i)
			{
				dragPointLogs[i + 1] = dragPointLogs[i];
			}
			dragPointLogs[0].point = startOffset + point;
			dragPointLogs[0].deltaTime = Time.deltaTime;

			if (dragPointNum > 1)
			{
				dragPointLogs[0].InitDirection(dragPointLogs[1].point);
			}
			else
			{
				dragPointLogs[0].ClearDirection();
			}

			dragDirection = Vector2.zero;
			dragSpeed = 0;
			int num = dragPointNum - 1;
			if (num > 0)
			{
				for (int i = 0; i < num; ++i)
				{
					dragDirection += dragPointLogs[i].direction * dragPointLogs[i].speed * dragPointLogs[i].deltaTime;
					dragSpeed += dragPointLogs[i].speed;
				}
				dragDirection.Normalize();
				dragSpeed /= num;
			}
		}

		//指定位置のコライダーのうち、もっともZ値が低いものを取得
		GameObject CheckPoint(float x, float y, int layerMask)
		{
			Collider2D hit = Physics2D.OverlapPoint(new Vector2(x, y), layerMask);
			if (null != hit)
			{
				return hit.gameObject;
			}
			else
			{
				return null;
			}
		}
		void Send(string func)
		{
			target.SendMessage(func, this, SendMessageOptions.DontRequireReceiver);
		}
	}

}