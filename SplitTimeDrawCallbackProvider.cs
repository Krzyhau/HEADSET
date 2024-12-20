using Microsoft.Xna.Framework;

namespace HEADSET
{
    internal class SplitTimeDrawCallbackProvider
    {
        private readonly Action<GameTime> drawCallback;

        int callsRequested;
        TimeSpan remainingTime;
        TimeSpan totalTime;

        public SplitTimeDrawCallbackProvider(Action<GameTime> drawCallback, GameTime gameTime)
        {
            this.drawCallback = drawCallback;
            totalTime = gameTime.TotalGameTime;
            remainingTime = gameTime.ElapsedGameTime;
        }

        public Action RequestCallback()
        {
            callsRequested++;
            return SplitTimeDrawCallback;
        }

        private void SplitTimeDrawCallback()
        {
            TimeSpan deltaTimeSpan = new TimeSpan(remainingTime.Ticks / callsRequested);
            drawCallback(new GameTime(totalTime, deltaTimeSpan));
            totalTime += deltaTimeSpan;
            remainingTime -= deltaTimeSpan;

            callsRequested--;
        }

    }
}
