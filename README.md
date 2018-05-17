# Notice
This is a fork, check out the original repository; https://github.com/MonoGame/MonoGame
(This "README.md" has been edited.)

# MonoGame

One framework for creating powerful cross-platform games.  The spiritual successor to XNA with 1000's of titles shipped across desktop, mobile, and console platforms.  [MonoGame](http://www.monogame.net/) is a fully managed .NET open source game framework without any black boxes.  Create, develop and distribute your games your way.


## Supported Platforms

 * Desktop PCs
   * Windows 10 Store Apps (UWP)
   * Windows Win32 (OpenGL & DirectX)
   * Linux (OpenGL)
   * Mac OS X (OpenGL)
 * Mobile/Tablet Devices
   * Android (OpenGL)
   * iPhone/iPad (OpenGL)
   * Windows Phone 10 (UWP)
 * Consoles (for registered developers)
   * PlayStation 4
   * PlayStation Vita
   * Xbox One (both UWP and XDK)
   * Nintendo Switch
 * Other
   * tvOS (OpenGL)


## Source Code

For the prerequisites for building from source please look at the [Requirements](REQUIREMENTS.md) file.

A high level breakdown of the components of the framework:

 * The game framework is found in [MonoGame.Framework](MonoGame.Framework).
 * The content pipeline is located in [MonoGame.Framework.Content.Pipeline](MonoGame.Framework.Content.Pipeline).
 * The MonoDevelop addin is in [IDE/MonoDevelop](IDE/MonoDevelop).
 * The Visual Studio templates are in [ProjectTemplates](ProjectTemplates).
 * NuGet packages are located in [NuGetPackages](NuGetPackages).
 * See [Test](Test) for the pipeline and framework unit tests.
 * [Tools/MGCB](Tools/MGCB) is the command line tool for content processing.
 * [Tools/2MGFX](Tools/2MGFX) is the command line effect compiler tool.
 * The [Tools/Pipeline](Tools/Pipeline) tool is a GUI frontend for content processing.


## Helpful Links

 * The official website is [monogame.net](http://www.monogame.net).
 * Our [issue tracker](https://github.com/MonoGame/MonoGame/issues) is on GitHub.
 * Use our [community forums](http://community.monogame.net/) for support questions.
 * You can [chat live](https://gitter.im/mono/MonoGame?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge&utm_content=badge) with the core developers and other users.
 * The [official documentation](http://www.monogame.net/documentation/) is on our website.
 * Download release and development [installers and packages](http://www.monogame.net/downloads/).
 * Follow [@MonoGameTeam](https://twitter.com/monogameteam) on Twitter.

## License

The MonoGame project is under the [Microsoft Public License](https://opensource.org/licenses/MS-PL) except for a few portions of the code.  See the [LICENSE.txt](LICENSE.txt) file for more details.  Third-party libraries used by MonoGame are under their own licenses.  Please refer to those libraries for details on the license they use.
