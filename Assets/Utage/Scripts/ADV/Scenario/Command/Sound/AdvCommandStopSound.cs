//----------------------------------------------
// UTAGE: Unity Text Adventure Game Engine
// Copyright 2014 Ryohei Tokimura
//----------------------------------------------


namespace Utage
{

	/// <summary>
	/// コマンド：サウンド停止
	/// </summary>
	internal class AdvCommandStopSound : AdvCommand
	{
		public AdvCommandStopSound(StringGridRow row)
		{
			this.fadeTime = AdvParser.ParseCellOptional<float>(row, AdvColumnName.Arg6, fadeTime);
		}

		public override void DoCommand(AdvEngine engine)
		{
			engine.SoundManager.Stop(SoundManager.StreamType.Bgm, fadeTime);
			engine.SoundManager.Stop(SoundManager.StreamType.Ambience, fadeTime);
		}

		float fadeTime = 0.15f;
	}
}