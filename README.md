# Speedrun Load Remover
C# app to remove loading times from speedrun videos using computer vision.
Powered by [Emgu CV](http://www.emgu.com), [libvideo](http://github.com/i3arnon/libvideo), [YoutubeExplode](http://github.com/Tyrrrz/YoutubeExplode).
## How to use
1. Open your video file or paste your YouTube video ID (e.g. C_VheAwZBuQ).
2. Navigate to and mark the time start and time end frames of the speedrun, effectively getting the RTA time.
3. Select an appropriate subregion of your video (What makes an appropriate subregion?).
4. Navigate to and mark the frame to be used as the template for the game's loading screen.
5. Calculate the loadless time.
## Download
[Version 1.0.0](https://github.com/nickbrick/SpeedrunLoadRemover/releases/tag/1.0.0)
Windows 32 and 64 bit.
