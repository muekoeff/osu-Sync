using System;
using System.Windows;
using System.Reflection;
using System.Runtime.InteropServices;

// Allgemeine Informationen über eine Assembly werden über die folgenden 
// Attribute gesteuert. Ändern Sie diese Attributwerte, um die Informationen zu ändern,
// die mit einer Assembly verknüpft sind.

// Die Werte der Assemblyattribute überprüfen

[assembly: AssemblyTitle("osu!Sync")]
[assembly: AssemblyDescription("Share your beatmaps.")]
[assembly: AssemblyCompany("nw520 Technologies")]
[assembly: AssemblyProduct("osu!Sync")]
[assembly: AssemblyCopyright("2017 | Open-Source")]
[assembly: AssemblyTrademark("")]

[assembly: ComVisible(false)]
//Um mit dem Erstellen lokalisierbarer Anwendungen zu beginnen, legen Sie 
//<UICulture>ImCodeVerwendeteKultur</UICulture> in der VBPROJ-Datei
//in einer <PropertyGroup> fest.  Wenn Sie in den Quelldateien beispielsweise Deutsch 
//(Deutschland) verwenden, legen Sie <UICulture> auf "de-DE" fest.  Heben Sie dann die Auskommentierung
//des nachstehenden NeutralResourceLanguage-Attributs auf.  Aktualisieren Sie "en-US" in der nachstehenden Zeile,
//sodass es mit der UICulture-Einstellung in der Projektdatei übereinstimmt.

//<Assembly: NeutralResourcesLanguage("en-US", UltimateResourceFallbackLocation.Satellite)> 


//Das ThemeInfo-Attribut beschreibt, wo Sie designspezifische und generische Ressourcenwörterbücher finden.
//1. Parameter: Speicherort der designspezifischen Ressourcenwörterbücher
//(wird verwendet, wenn eine Ressource auf der Seite 
// oder in den Anwendungsressourcen-Wörterbüchern nicht gefunden werden kann.)

//2. Parameter: Speicherort des generischen Ressourcenwörterbuchs
//(wird verwendet, wenn eine Ressource auf der Seite, 
//in der Anwendung sowie in den designspezifischen Ressourcenwörterbüchern nicht gefunden werden kann)

[assembly: ThemeInfo(ResourceDictionaryLocation.None, ResourceDictionaryLocation.SourceAssembly)]


//Die folgende GUID bestimmt die ID der Typbibliothek, wenn dieses Projekt für COM verfügbar gemacht wird

[assembly: Guid("da8900bf-73d7-45cb-a805-3ea353a3bb2d")]
// Versionsinformationen für eine Assembly bestehen aus den folgenden vier Werten:
//
//      Hauptversion
//      Nebenversion 
//      Buildnummer
//      Revision
//
// Sie können alle Werte angeben oder die standardmäßigen Build- und Revisionsnummern 
// übernehmen, indem Sie "*" eingeben:
// <Assembly: AssemblyVersion("1.0.*")> 

[assembly: AssemblyVersion("1.0.0.16")]
[assembly: AssemblyFileVersion("1.0.0.16")]
