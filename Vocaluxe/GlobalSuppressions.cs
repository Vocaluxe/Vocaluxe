// Diese Datei wird von der Codeanalyse zur Wartung der SuppressMessage- 
// Attribute verwendet, die auf dieses Projekt angewendet werden.
// Unterdrückungen auf Projektebene haben entweder kein Ziel oder 
// erhalten ein spezifisches Ziel mit Namespace-, Typ-, Memberbereich usw.
//
// Wenn Sie dieser Datei eine Unterdrückung hinzufügen möchten, klicken Sie mit der 
// rechten Maustaste auf die Meldung in der Fehlerliste, zeigen Sie auf 
// "Meldung(en) unterdrücken", und klicken Sie auf "In Projektunterdrückungsdatei".
// Sie müssen dieser Datei nicht manuell Unterdrückungen hinzufügen.
using System.Diagnostics.CodeAnalysis;

[assembly:
    SuppressMessage("Microsoft.Reliability", "CA2001:AvoidCallingProblematicMethods", MessageId = "System.Reflection.Assembly.LoadFrom", Scope = "member",
        Target = "Vocaluxe.MainProgram.#AssemblyResolver(System.Object,System.ResolveEventArgs)")]
[assembly:
    SuppressMessage("Microsoft.Reliability", "CA2000:Objekte verwerfen, bevor Bereich verloren geht", Scope = "member",
        Target = "Vocaluxe.Lib.Draw.CDrawWinForm.#ColorizeBitmap(System.Drawing.Bitmap,Vocaluxe.Menu.SColorF)")]
[assembly:
    SuppressMessage("Microsoft.Reliability", "CA2000:Objekte verwerfen, bevor Bereich verloren geht", Scope = "member",
        Target = "Vocaluxe.Lib.Draw.CDrawWinForm.#CopyScreen(Vocaluxe.Menu.STexture&)")]
[assembly:
    SuppressMessage("Microsoft.Reliability", "CA2000:Objekte verwerfen, bevor Bereich verloren geht", Scope = "member",
        Target = "Vocaluxe.Base.CGlyph.#.ctor(System.Char,System.Single)")]
[assembly:
    SuppressMessage("Microsoft.Reliability", "CA2000:Objekte verwerfen, bevor Bereich verloren geht", Scope = "member",
        Target = "Vocaluxe.Lib.Draw.CDirect3D.#AddTexture(System.Drawing.Bitmap,System.String)")]
[assembly:
    SuppressMessage("Microsoft.Reliability", "CA2000:Objekte verwerfen, bevor Bereich verloren geht", Scope = "member",
        Target = "Vocaluxe.Lib.Draw.CDirect3D.#AddTexture(System.Int32,System.Int32,System.Byte[]&)")]
[assembly: SuppressMessage("Microsoft.Reliability", "CA2000:Objekte verwerfen, bevor Bereich verloren geht", Scope = "member", Target = "Vocaluxe.Lib.Draw.CDirect3D.#CopyScreen()")
]
[assembly: SuppressMessage("Microsoft.Design", "CA1008:EnumsShouldHaveZeroValue", Scope = "type", Target = "Vocaluxe.Lib.Input.WiiMote.InputReport")]
[assembly: SuppressMessage("Microsoft.Design", "CA1008:EnumsShouldHaveZeroValue", Scope = "type", Target = "PortAudioSharp.PortAudio+PaDeviceIndex")]
[assembly: SuppressMessage("Microsoft.Design", "CA1008:EnumsShouldHaveZeroValue", Scope = "type", Target = "PortAudioSharp.PortAudio+PaSampleFormat")]
[assembly: SuppressMessage("Microsoft.Design", "CA1008:EnumsShouldHaveZeroValue", Scope = "type", Target = "PortAudioSharp.PortAudio+PaStreamCallbackFlags")]
[assembly:
    SuppressMessage("Microsoft.Security", "CA2122:DoNotIndirectlyExposeMethodsWithLinkDemands", Scope = "member",
        Target = "Vocaluxe.Lib.Video.Gstreamer.CGstreamerVideoWrapper.#GetFrame(System.Int32,System.Single)")]
[assembly:
    SuppressMessage("Microsoft.Security", "CA2122:DoNotIndirectlyExposeMethodsWithLinkDemands", Scope = "member",
        Target = "Vocaluxe.Lib.Input.CHIDAPI.#ReadTimeout(System.IntPtr,System.Byte[]&,System.Int32,System.Int32)")]
[assembly:
    SuppressMessage("Microsoft.Security", "CA2122:DoNotIndirectlyExposeMethodsWithLinkDemands", Scope = "member",
        Target = "Vocaluxe.Lib.Input.CHIDAPI.#Read(System.IntPtr,System.Byte[]&,System.Int32)")]
[assembly: SuppressMessage("Microsoft.Security", "CA2122:DoNotIndirectlyExposeMethodsWithLinkDemands", Scope = "member", Target = "PortAudioSharp.PortAudio.#Pa_GetVersionText()")]
[assembly:
    SuppressMessage("Microsoft.Security", "CA2122:DoNotIndirectlyExposeMethodsWithLinkDemands", Scope = "member",
        Target = "PortAudioSharp.PortAudio.#Pa_GetErrorText(PortAudioSharp.PortAudio+PaError)")]
[assembly:
    SuppressMessage("Microsoft.Security", "CA2122:DoNotIndirectlyExposeMethodsWithLinkDemands", Scope = "member",
        Target = "PortAudioSharp.PortAudio.#Pa_GetHostApiInfo(System.Int32)")]
[assembly:
    SuppressMessage("Microsoft.Security", "CA2122:DoNotIndirectlyExposeMethodsWithLinkDemands", Scope = "member", Target = "PortAudioSharp.PortAudio.#Pa_GetLastHostErrorInfo()")]
[assembly:
    SuppressMessage("Microsoft.Security", "CA2122:DoNotIndirectlyExposeMethodsWithLinkDemands", Scope = "member",
        Target = "PortAudioSharp.PortAudio.#Pa_GetDeviceInfo(System.Int32)")]
[assembly:
    SuppressMessage("Microsoft.Security", "CA2122:DoNotIndirectlyExposeMethodsWithLinkDemands", Scope = "member",
        Target = "PortAudioSharp.PortAudio.#Pa_GetStreamInfo(System.IntPtr)")]