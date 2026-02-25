PSA v4 Virtual Printer Samples
====

This project provides sample code for building a virtual printer using Print Support Application v4 (PSA v4) that supports [Windows Protected Print Mode (WPP)](https://learn.microsoft.com/en-us/windows/modern-print/windows-protected-print-mode/windows-protected-print-mode).

Currently, two samples are provided: [Minimal](https://github.com/cube-soft/cube.psa.samples/tree/master/Samples/Minimal), which represents the minimum configuration, and [DesktopBridge](https://github.com/cube-soft/cube.psa.samples/tree/master/Samples/DesktopBridge), which is an implementation that uses the Desktop Bridge feature. In both samples, the application side simply saves the print data received from the virtual printer to the specified path. When using these samples in practice, you must implement your own PDF conversion or similar processing. The [CubePDF](https://github.com/cube-soft/cube.pdf) repository may be helpful as a reference.

Note that in the current DesktopBridge sample, printing multiple jobs consecutively before the final GUI application exits may cause data consistency issues. We are continuing to investigate how to handle consecutive print jobs safely.

## Background

Windows Protected Print Mode was introduced in Windows 11 24H2, and its future default enablement has been announced ([the specific date is currently unknown](https://learn.microsoft.com/en-us/windows/modern-print/windows-protected-print-mode/windows-protected-mode-faq)). However, regarding the construction of virtual printers using Print Support Application v4 (PSA v4), the technology required to support WPP, only the following high-level overview document is available. Strict specifications and sample code required to actually output files such as PDFs are effectively not publicly available.

[Print Support App v4 API design guide](https://learn.microsoft.com/en-us/windows-hardware/drivers/devapps/print-support-app-v4-design-guide)

Based on our investigation of publicly available information as well as major OSS and commercial PDF virtual printer products, we recognize only one implementation of a PSA v4 virtual printer capable of outputting PDFs while WPP is enabled: the PDF printer provided by Adobe Acrobat. The built-in Windows feature Microsoft Print to PDF is widely used for PDF conversion, but it appears to be implemented using a structure different from that of typical third-party virtual printers (at least, it does not match the configuration described in the PSA v4 design guide). As a result, in WPP-enabled environments, PSA v4 virtual printers do not appear to be sufficiently validated in real-world usage scenarios.

Introducing new technologies is not inherently problematic. However, if existing mechanisms are being gradually disabled and migration is expected under the assumption of future default enablement, then providing migration paths, technical guidance, and sample implementations should be part of the OS vendor’s responsibility. At present, this prerequisite does not appear to be satisfied.

For example, on [Microsoft’s forum](https://learn.microsoft.com/answers/questions/2200367/), responses indicate that while multiple inquiries exist regarding building virtual printers with PSA v4, documentation updates have not been provided. We have also contacted Microsoft through multiple channels in both Japan and the United States while conducting our own investigation and development, but have not received any concrete technical guidance regarding implementation. Regardless of intent, the current situation can be perceived as effectively neglecting—or even excluding—third-party developers.

For these reasons, we decided to independently investigate PSA v4 virtual printers and publish sample code alongside development of a WPP-compatible experimental version of CubePDF. This project aims to serve as a reference for developers facing similar challenges.

## Notes for Virtual Printer Development

Below are items we actually struggled with while developing PSA v4 virtual printers. The PSA v4 design guide appears to assume familiarity with all related technologies and provides no references to them. When developing WinRT components in C#/.NET, understanding the following project is essential.

[The C#/WinRT Language Projection (CsWinRT)](https://github.com/microsoft/CsWinRT)

### Disabling SelfContained and Manual Packaging

This section still contains unresolved points even in the published projects. When creating a DLL that behaves as a WinRT component in .NET (including PSA v4 virtual printers), SelfContained must be disabled ([Self-contained WinRT components don't work](https://github.com/microsoft/CsWinRT/issues/1141)).

However, when building packages via Visual Studio’s MSIX packaging project, SelfContained is forcibly enabled regardless of project settings. Therefore, although we include MSIX packaging projects, actual packaging must currently be performed manually via the command line. (If you know a better solution, pull requests are welcome.)

For example, to create a package for the Minimal project, arrange the files in a working directory as shown below: The AppxManifest.xml and resources.pri files should be obtained from the Packaging\bin\x64\Release folder after building the [Packaging project](https://github.com/cube-soft/cube.psa.samples/blob/master/Samples/Minimal/Packaging). Please note that depending on the timing of the build process, the output may have been generated with SelfContained enabled, so be sure to verify this before proceeding.

```
work
 + bin
     + Cube.Psa.Minimal.Main
         - contents of Main\bin\x64\Release\net8.0-windows10.0.26100.0\win-x64
     + Assets
         - contents of Packaging\Assets
     - AppxManifest.xml
     - resources.pri
 - Test.pfx
```

Then build and sign:

```
$ makeappx pack /d bin /p out.msix
$ signtool sign /fd SHA256 /a /f Test.pfx /p password out.msix
```

Test.pfx is for testing only (password is "password"). Use a proper certificate for real distribution and update the Publisher attribute accordingly.

### Project File (.csproj) Settings

Since PSA v4 virtual printers were introduced in Windows 11 24H2, each *.csproj file must be configured accordingly. Specifically, the settings should be as follows:

```
<TargetFramework>net8.0-windows10.0.26100.0</TargetFramework>
<TargetPlatformMinVersion>10.0.26100.0</TargetPlatformMinVersion>
<TargetPlatformVersion>10.0.26100.0</TargetPlatformVersion>
```

In addition, when creating a DLL that behaves as a WinRT component (such as the various VirtualPrinter projects), you must add a reference to [Microsoft.Windows.CsWinRT](https://www.nuget.org/packages/microsoft.windows.cswinrt/).

```
<ItemGroup>
  <PackageReference Include="Microsoft.Windows.CsWinRT" Version="2.2.0" />
</ItemGroup>
```

Without this reference, builds may succeed, but runtime errors will occur.

### Package.appxmanifest Settings

The biggest obstacle we encountered while developing PSA v4 virtual printers was creating the Package.appxmanifest for the MSIX installer. In this section, we list the items that actually caused us trouble.

#### DisplayName Must Be a PRI Resource

For the DisplayName attribute of the printsupport2:PrintSupportVirtualPrinter tag, which specifies the virtual printer name, you must not write the printer name directly. Instead, it must be defined as a string resource inside a resource file (*.pri).

Including cases reported on [Microsoft’s forums](https://learn.microsoft.com/en-us/answers/questions/2200367/), there are many examples of developers getting stuck because they specified DisplayName directly, making this one of the first major stumbling blocks. The design guide does not mention this restriction at all, and even when DisplayName is written directly, both the build and installation complete successfully (however, the printer is not actually created). This behavior makes it extremely difficult to identify the root cause.

#### PrinterUri Is Required

The PrinterUri attribute of the printsupport2:PrintSupportVirtualPrinter tag is required.

The design guide states, "If URI isn't specified, Windows assigns an arbitrary unique URI to the printer." However, in practice, although installation succeeds, omitting PrinterUri results in a runtime error.

As far as we can tell, the actual value does not matter as long as it is unique.

#### PrintSupportExtension Is Also Required

PSA v4 virtual printers define the following four extension categories:

* windows.printSupportVirtualPrinterWorkflow
* windows.printSupportExtension
* windows.printSupportSettingsUI
* windows.printSupportJobUI

The design guide does not state which of these are required. However, in practice, in addition to printSupportVirtualPrinterWorkflow, printSupportExtension is also required. If printSupportExtension is not specified, installation succeeds, but a runtime error occurs.

We do not yet fully understand how printSupportExtension is supposed to be implemented. However, using the following code to unconditionally set WorkflowPrintTicketValidationStatus.Resolved appears to work:

```cs
public sealed class PsaExtensionTask : IBackgroundTask
{
    public void Run(IBackgroundTaskInstance task)
    {
        var deferral = task?.GetDeferral();
        if (task is null || deferral is null) return;
        task.Canceled += (_, _) => deferral.Complete();

        var details = task.TriggerDetails as PrintSupportExtensionTriggerDetails;
        if (details?.Session is null)
        {
            deferral.Complete();
            return;
        }

        details.Session.PrintTicketValidationRequested += (_, e) =>
        {
            using (e.GetDeferral()) e.SetPrintTicketValidationStatus(WorkflowPrintTicketValidationStatus.Resolved);
        };

        details.Session.Start();
    }
}
```

#### ActivatableClass Must Be Declared

Although it is not mentioned in the design guide, specifying windows.activatableClass.inProcessServer is required for PSA v4 virtual printers to function properly. If this entry is omitted, installation succeeds, but a runtime error occurs. For example, in the case of the Minimal project, it should be specified as follows:

```
<Extensions>
  <Extension Category="windows.activatableClass.inProcessServer">
    <InProcessServer>
    <Path>Cube.Psa.Minimal.Main\WinRT.Host.dll</Path>
    <ActivatableClass ActivatableClassId="Cube.Psa.Minimal.VirtualPrinter.PsaExtensionTask" ThreadingModel="both" />
    <ActivatableClass ActivatableClassId="Cube.Psa.Minimal.VirtualPrinter.PsaVirtualPrinterTask" ThreadingModel="both" />
    </InProcessServer>
  </Extension>
</Extensions>
```

As a point of caution, as also described in the [CsWinRT Background Task Sample](https://github.com/microsoft/CsWinRT/tree/master/src/Samples/BgTaskComponent), the value specified for Path must not be your own DLL. Instead, it must be the WinRT.Host.dll included in the build output.

In addition, it appears safer to keep the namespace of each class used as an IBackgroundTask implementation the same as the DLL name. For the rules used to locate the DLL based on values such as ActivatableClassId, please also refer to [Managed Component Hosting](https://github.com/microsoft/CsWinRT/blob/master/docs/hosting.md).

#### SupportsMultipleInstances Must Be Enabled

To install a PSA v4 virtual printer, the SupportsMultipleInstances attribute must be enabled. There appear to be two variants: uap10:SupportsMultipleInstances and desktop4:SupportsMultipleInstances. Based on our testing, either works. The sample programs use uap10:SupportsMultipleInstances. For more details, please also refer to [Create a multi-instance Universal Windows App](https://learn.microsoft.com/en-us/windows/uwp/launch-resume/multi-instance-uwp). In addition, when adding namespaces such as uap10 or desktop4, they must also be included in the [IgnorableNamespaces](https://learn.microsoft.com/ja-jp/uwp/schemas/appxpackage/uapmanifestschema/element-package) attribute.

#### PublisherCacheFolders (for DesktopBridge sample)

In the DesktopBridge sample, the PublisherCacheFolders feature is used to share input print data between multiple processes. To use this feature, it must be declared in advance in Package.appxmanifest. For example, to use a shared folder named printing, specify it as follows:

```
<Extensions>
  <Extension Category="windows.publisherCacheFolders">
    <PublisherCacheFolders>
      <Folder Name="printing" />
    </PublisherCacheFolders>
  </Extension>
</Extensions>
```

On the C#/.NET side, the shared folder can be accessed using the following code:

```cs
var dir = ApplicationData.Current.GetPublisherCacheFolder("printing");
```

#### About PDC Files

For PSA v4 virtual printers, the configuration of the created printer appears to be described in a PDC file. However, the design guide does not mention or link to any documentation regarding the PDC file format or schema, and we were unable to find any official specification at this time. The [Printer.pdc](https://github.com/cube-soft/cube.psa.samples/blob/master/Samples/Minimal/Packaging/Assets/Printer.pdc) file provided in the sample code was created with only the minimum required elements, based on observed behavior and with reference to Microsoft’s publicly available [Print Schema documentation](https://learn.microsoft.com/ja-jp/windows/win32/printdocs/printcapabilities-public-keywords).

We were also unable to find specifications for PDR files. However, since they are optional, we currently ignore them.

#### Uninstall / Update Issues

Currently, PSA v4 virtual printers can only be installed using an MSIX installer. However, we have observed that they are not completely removed during uninstall. To fully uninstall a PSA v4 virtual printer, first perform the standard uninstall using Windows, then open the Registry Editor, delete the subkey that is assumed to correspond to the virtual printer port under the following location, and reboot the system.

```
HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Print\Monitors\Virtual Port Monitor\Ports
```

For example, when installing the virtual printer of the Minimal project in a development environment, a port with the following name was created:

```
CubePsaMinimal_re2h7yn3p4en2_cube-psa-minimal:ps-printer_S-1-5-21-3114412831-3744230469-3766615254-1003
```

In addition, when you modify the Package.appxmanifest (AppxManifest.xml) file during development, the changes may not be reflected even after uninstalling and reinstalling the package. In such cases as well, deleting the corresponding subkey manually after uninstalling and then rebooting the system may resolve the issue.


## Conclusion

The above describes the results of our analysis and investigation of PSA v4 virtual printers to date. Much of the content published here is based on inference from real-machine testing, so it may contain inaccuracies. If you find any errors or have additional information, we would appreciate it if you could submit a [Pull Request (PR) to this project on GitHub](https://github.com/cube-soft/cube.psa.samples).

Our honest impression is that this work would be trivial for anyone who already knows the specifications. However, at present, almost no official information is provided to enable developers to understand those specifications. As a result, due to unclear and ambiguous specifications, we were forced to rely on trial-and-error verification, which required an enormous amount of time.

We sincerely hope this documentation helps developers who are struggling with the same problems.