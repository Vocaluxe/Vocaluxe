#region license
// /*
//     This file is part of Vocaluxe.
// 
//     Vocaluxe is free software: you can redistribute it and/or modify
//     it under the terms of the GNU General Public License as published by
//     the Free Software Foundation, either version 3 of the License, or
//     (at your option) any later version.
// 
//     Vocaluxe is distributed in the hope that it will be useful,
//     but WITHOUT ANY WARRANTY; without even the implied warranty of
//     MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//     GNU General Public License for more details.
// 
//     You should have received a copy of the GNU General Public License
//     along with Vocaluxe. If not, see <http://www.gnu.org/licenses/>.
//  */
#endregion

using VocaluxeLib.Draw;

namespace Vocaluxe.Lib.Video
{
    interface IVideoDecoder
    {
        /// <summary>
        /// Initializes videodecoder. Got to be called before usage
        /// </summary>
        /// <returns>True on success, false if something went wrong.</returns>
        bool Init();

        /// <summary>
        /// Close all video streams
        /// </summary>
        void CloseAll();

        /// <summary>
        /// Loads the file with the given file path.
        /// </summary>
        /// <param name="videoFileName">Absolute file path of the video file</param>
        /// <returns>StreamID &gt;=0 on success or -1 on failure</returns>
        int Load(string videoFileName);

        /// <summary>
        /// Closes the stream with the given ID
        /// </summary>
        /// <param name="streamID">ID of the stream</param>
        /// <returns>true if stream was closed, false if stream doesn't exist or already closed</returns>
        bool Close(int streamID);

        /// <summary>
        /// Gets the number of streams currently active
        /// </summary>
        /// <returns></returns>
        int GetNumStreams();

        /// <summary>
        /// Gets the length of the video in s
        /// </summary>
        /// <param name="streamID">Id of the stream</param>
        /// <returns>Length of the video in s</returns>
        float GetLength(int streamID);

        /// <summary>
        /// Gets a frame of the video and sets the current video time
        /// </summary>
        /// <param name="streamID">Id of the stream</param>
        /// <param name="frame">Reference to already initialized video texture (from previous calls). If null, one will be created.<br/>
        /// Returns the current video frame which might be outdated if time != current (internal) video position or unchanged (even null) if none is available.</param>
        /// <param name="time">The position in s of the video you want to get a frame of. If != current (internal) video position, it will seek to get to right position.<br/>
        /// <b>Note:</b>Will be ignored for looped playback!</param>
        /// <param name="videoTime">The actual position in s of the returned frame. Should be ~ time+VideoGap</param>
        /// <returns>True if frame is valid, false: frame and videoTime might be unchanged or invalid</returns>
        bool GetFrame(int streamID, ref CTexture frame, float time, out float videoTime);

        /// <summary>
        /// Seeks to given position
        /// </summary>
        /// <param name="streamID">Id of the stream</param>
        /// <param name="start">Position to seek to</param>
        /// <param name="gap">New video gap -> Actual position will be start+gap</param>
        /// <returns>True on success</returns>
        bool Skip(int streamID, float start, float gap);

        /// <summary>
        /// Set if the video should be looped
        /// </summary>
        /// <param name="streamID">Id of the stream</param>
        /// <param name="loop">True if it should be looped</param>
        void SetLoop(int streamID, bool loop);

        /// <summary>
        /// Pauses the video (no new frames will be decoded)
        /// </summary>
        /// <param name="streamID">Id of the stream</param>
        void Pause(int streamID);

        /// <summary>
        /// Resumes a paused video
        /// </summary>
        /// <param name="streamID">Id of the stream</param>
        void Resume(int streamID);

        /// <summary>
        /// Check if the given video is finished
        /// </summary>
        /// <param name="streamID">Id of the stream</param>
        /// <returns>True if all frames are shown</returns>
        bool Finished(int streamID);

        /// <summary>
        /// Do a step of the decoder in unthreaded decoders. Should be called once every frame.
        /// </summary>
        void Update();
    }
}