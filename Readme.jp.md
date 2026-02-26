PSA v4 Virtual Printer Samples
====

本プロジェクトは、[Windows 保護印刷モード (WPP: Windows Protected Print Mode)](https://learn.microsoft.com/ja-jp/windows/modern-print/windows-protected-print-mode/windows-protected-print-mode) に対応した仮想プリンターを構築するためのサンプルコードです。

尚、サンプルプログラムは SelfContained の無効化が必須となるため、実行時に .NET 8.0 デスクトップランタイムが必要になります。.NET 8.0 デスクトップランタイムは、下記 URL よりダウンロードして下さい。

[.NET 8.0 のダウンロード](https://dotnet.microsoft.com/ja-jp/download/dotnet/8.0)

現在は、最小構成である [Minimal](https://github.com/cube-soft/cube.psa.samples/tree/master/Samples/Minimal)、および DesktopBridge 機能を利用した実装である [DesktopBridge](https://github.com/cube-soft/cube.psa.samples/tree/master/Samples/DesktopBridge) の 2 種類を公開しています。尚、どちらもアプリケーション側の処理は、仮想プリンター側から渡された印刷データを指定されたパスに保存するのみとなっています。そのため、実際に使用する際には PDF への変換処理等を別途実装する必要があります。PDF への変換処理については、[CubePDF リポジトリ](https://github.com/cube-soft/cube.pdf) 等も参考になる可能性があります。また、DesktopBridge 版に関しては、現行バージョンでは最終的な GUI アプリが終了する前に、連続して印刷を行うとデータの整合性がおかしくなりますので、ご注意ください。連続して印刷した際に整合性を保つ実装に関しては、引き続き検討を行っていきます。

## 公開の背景

Windows では、Windows 11 24H2 にて導入された Windows 保護印刷モードの将来的な既定有効化が予告されています（[具体的な日程は不明](https://learn.microsoft.com/ja-jp/windows/modern-print/windows-protected-print-mode/windows-protected-mode-faq)）。しかし、これに対応するための技術である Print Support Application v4 (PSA v4) を用いた仮想プリンターの構築方法については、下記の概要文書が公開されているのみで、実装に必要となる厳密な仕様や PDF 等のファイルとして出力するためのサンプルコードは事実上公開されていません。

[Print Support App v4 API design guide](https://learn.microsoft.com/en-us/windows-hardware/drivers/devapps/print-support-app-v4-design-guide)

弊社が公開情報および主要 OSS / 商用 PDF 仮想プリンター製品を調査した限りでは、Windows 保護印刷モードを有効化したまま動作する PDF 出力用 PSA v4 仮想プリンターの実装事例は、Adobe Acrobat から提供される PDF プリンターの 1 例のみであると認識しています。また、Windows の標準機能である Microsoft Print to PDF は、PDF への変換用途として広く利用されている機能ですが、一般的なサードパーティー製の仮想プリンターとは構造が異なる実装になっていると推測されます（少なくとも、弊社で確認した限りでは PSA v4 のデザインガイドで示されている構成とは異なっています）。その結果、Windows 保護印刷モードが有効化された環境において、PSA v4 仮想プリンターが実際の利用シナリオにおいて十分に検証されているとは言い難い状況が続いています。

新技術を導入すること自体に異論はありませんが、既存の仕組みを段階的に無効化し、将来的な既定有効化を前提として移行を求めるのであれば、移行パスや技術的な指針、サンプル実装を公開し、移行可能な状態を整備することも OS ベンダーである Microsoft 社の役割と考えられます。しかし、少なくとも現時点では、その前提が満たされているとは言えません。

例えば、[Microsoft のフォーラム](https://learn.microsoft.com/en-us/answers/questions/2200367/create-virtual-printer-using-print-support-app-(ps) においても、「PSA v4 を用いた仮想プリンターの構築に関する問い合わせが複数確認されているものの、ドキュメントの更新等は行われていない」旨の回答が投稿されています。弊社も、自社による検証・開発と並行して、日米問わず複数の経路から Microsoft 社へ問い合わせを行いましたが、具体的な実装方法等に関する技術的な回答は何も得られていません。現状の対応は、意図の如何に関わらず、結果としてサードパーティーの軽視、あるいは排除と受け取られかねない状況を生んでいるように見受けられます。

このような背景から、弊社では独自に PSA v4 仮想プリンターの検証を行い、CubePDF の Windows 保護印刷モード対応版の開発と並行して、サンプルコードを公開することにしました。本プロジェクトは、同様の課題に直面している開発者の参考となることを目的としています。

## 仮想プリンター開発における注意点

ここからは、PSA v4 仮想プリンターの開発を進める際に、実際に躓いた項目を挙げていきます。尚、PSA v4 のデザインガイドでは、直接関係のあるもの以外は全て把握済みと言う前提なのか、実際に必要となる関連技術への言及はリンク誘導も含めて一切なされていませんが、C#/.NET で WinRT コンポーネントを開発する上では、下記プロジェクトの理解が必須となります。

[The C#/WinRT Language Projection (CsWinRT)](https://github.com/microsoft/CsWinRT)

### SelfContained 設定の無効化と手動パッケージング

まず、この項目に関しては公開しているプロジェクトにおいても未解決な点がありますので、最初に記載します。PSA v4 仮想プリンターを始め、.NET で WinRT コンポーネントとして振る舞う DLL を作成する場合、SelfContained は無効にする必要があります（参考: [Self-contained WinRT components don't work](https://github.com/microsoft/CsWinRT/issues/1141)）。

しかし、現在 Visual Studio より提供されている MSIX インストーラー用のプロジェクト経由でパッケージを作成すると、SelfContained および関連項目の設定内容に関わらず、強制的に SelfContained が有効になる現象が確認されています。そのため、公開しているプロジェクトでは MSIX インストーラー用のプロジェクトも含めているものの、実際には手動でコマンドを打ってパッケージを作成する必要があります((この部分は、こちら側の設定ミスの可能性もありますので、改善できる方法があれば Pull Request 等を頂けると幸いです))。

例えば、Minimal プロジェクトのパッケージを作成するには、適当な作業フォルダー上で下記のようにファイルを配置します。AppxManifest.xml および resources.pri に関しては、[Packaging プロジェクト](https://github.com/cube-soft/cube.psa.samples/blob/master/Samples/Minimal/Packaging) をいったんビルドした上で、Packaging\bin\x64\Release フォルダーから取得して下さい。尚、操作のタイミングによっては SelfContained が有効になったビルド結果になっている事もありますので、ご注意下さい。

```
work
 + bin
     + Cube.Psa.Minimal.Main
         - Main\bin\x64\Release\net8.0-windows10.0.26100.0\win-x64 の内容
     + Assets
         - Packaging\Assets の内容
     - AppxManifest.xml
     - resources.pri
 - Test.pfx
```

そして、下記コマンドで *.msix ファイルを生成し、署名して下さい。

```
$ makeappx pack /d bin /p out.msix
$ signtool sign /fd SHA256 /a /f Test.pfx /p password out.msix
```

尚、Test.pfx はテスト用の証明書です（パスワードは "password"）。実際に仮想プリンターを開発して公開する際には、正規の証明書を取得する必要があります。証明書を差し替える際には、Package.appxmanifest ファイルの Identity タグ、Publisher 属性もそれに合わせて修正して下さい。

### プロジェクトファイル (.csproj) の設定

PSA v4 仮想プリンターは Windows 11 24H2 にて導入されたため、各 *.csproj ファイルもそれに合わせた設定にする必要があります。具体的には、下記のようになります。

```
<TargetFramework>net8.0-windows10.0.26100.0</TargetFramework>
<TargetPlatformMinVersion>10.0.26100.0</TargetPlatformMinVersion>
<TargetPlatformVersion>10.0.26100.0</TargetPlatformVersion>
```

また、これに加えて WinRT コンポーネントとして振る舞う DLL を作成する場合（各種 VirtualPrinter）、[Microsoft.Windows.CsWinRT](https://www.nuget.org/packages/microsoft.windows.cswinrt/) を参照に追加する必要があります。

```
<ItemGroup>
  <PackageReference Include="Microsoft.Windows.CsWinRT" Version="2.2.0" />
</ItemGroup>
```

Microsoft.Windows.CsWinRT は、プロジェクト側で使用する訳ではないので、この参照を追加しなくてもビルドおよびインストールは成功するのですが、生成される DLL が不完全なため実行時エラーとなります。

### Package.appxmanifest の設定

PSA v4 仮想プリンターの開発を進める上で、最大の障壁が MSIX インストーラー用の Package.appxmanifest の作成でした。ここでは、実際に躓いた項目を列挙していきます。

#### DisplayName は PRI リソース化が必須

仮想プリンター名となる printsupport2:PrintSupportVirtualPrinter タグの DisplayName 属性には、プリンター名をそのまま記述するのではなく、必ずリソースファイル (*.pri) に埋め込む必要があります。

[Microsoft のフォーラム](https://learn.microsoft.com/en-us/answers/questions/2200367/create-virtual-printer-using-print-support-app-(ps) で投稿されている事例を含め、DisplayName を直接記述したために躓いている事例が多数見受けられ、ここが最初の難関となっているようです。デザインガイドには特にそのような制限事項は記載されていない事や、直接記載してもビルドやインストールは正常に終了した事になる（ただし、プリンターは作成されない）のも、原因の特定を困難にしているようです。

#### PrinterUri は必須

printsupport2:PrintSupportVirtualPrinter タグの PrinterUri 属性は必須項目となります。

デザインガイドでは、PrinterUri に関しては "If URI isn't specified, Windows assigns an arbitrary unique URI to the printer." と言う記載があるのですが、実際にはインストールは成功するものの、実行時エラーとなります。指定する内容に関しては、恐らく一意であれば、何でも良いようです。

#### PrintSupportExtension も必須

PSA v4 仮想プリンターでは、下記の 4 つの Category が存在します。

* windows.printSupportVirtualPrinterWorkflow
* windows.printSupportExtension
* windows.printSupportSettingsUI
* windows.printSupportJobUI

デザインガイドでは各項目に対して必須かどうかの記述がないのですが、この内、printSupportVirtualPrinterWorkflow に加えて printSupportExtension も必須項目となります。printSupportExtension を指定しない場合、インストールは成功するものの、実行時エラーとなります。

printSupportExtension をどのように実装すれば良いのか正確には分かっていないのですが、下記のように、無条件で WorkflowPrintTicketValidationStatus.Resolved を設定するコードで成功するようです。

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

#### ActivatableClass の指定が必要

デザインガイドでは省略されていますが、PSA v4 仮想プリンターを動作させるには別途 windows.activatableClass.inProcessServer の指定が必要となります。指定しない場合、インストールは成功するものの、実行時エラーとなります。例えば Minimal プロジェクトの場合、指定方法は下記のようになります。

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

注意点としては、[CsWinRT Background Task Sample](https://github.com/microsoft/CsWinRT/tree/master/src/Samples/BgTaskComponent) にも記載されているように、Path に指定するものは、自分で作成した DLL ではなく、ビルド結果に含まれる WinRT.Host.dll にしなければならないと言う点です。

また、各種 IBackgroundTask 実装として使用するクラスの名前空間と DLL の名前は同じにしておく方が無難なようです。ActivatableClassId 等から DLL を検索するための規則に関しては、[Managed Component Hosting](https://github.com/microsoft/CsWinRT/blob/master/docs/hosting.md) も参照下さい。

#### SupportsMultipleInstances の有効化

PSA v4 仮想プリンターとしてインストールするには、SupportsMultipleInstances と言う属性を有効化する必要があるようです。SupportsMultipleInstances には、uap10:SupportsMultipleInstances と desktop4:SupportsMultipleInstances の 2 種類があるようなのですが、検証した限りではどちらでも動作しました。サンプルプログラムでは uap10:SupportsMultipleInstances の方を使用しています。詳細については、[複数インスタンスのユニバーサル Windows アプリを作成する](https://learn.microsoft.com/ja-jp/windows/uwp/launch-resume/multi-instance-uwp) も参照下さい。また、uap10 や desktop4 のような名前空間を追加した際には、その名前空間を [IgnorableNamespaces](https://learn.microsoft.com/ja-jp/uwp/schemas/appxpackage/uapmanifestschema/element-package) 属性に追加する必要があるようです。 

####  PublisherCacheFolders 機能の使用について

DesktopBridge 版のサンプルコードでは、入力データとなる印刷ファイルを複数プロセス間で共有するために PublisherCacheFolders と言う機能を利用しているのですが、この機能を利用するには Package.appxmanifest であらかじめ指定する必要があるようです。例えば、printing と言う共有フォルダーを利用する場合は、下記のように記述します。

```
<Extensions>
  <Extension Category="windows.publisherCacheFolders">
    <PublisherCacheFolders>
      <Folder Name="printing" />
    </PublisherCacheFolders>
  </Extension>
</Extensions>
```

C#/.NET 側では下記のようなコードで共有フォルダーにアクセスできます。

```cs
var dir = ApplicationData.Current.GetPublisherCacheFolder("printing");
```

### PDC ファイルについて

PSA v4 仮想プリンターでは、作成されるプリンターの設定内容を PDC ファイルに記載するようなのですが、PDC ファイルのフォーマットやスキーマに関してはデザインガイドで言及およびリンク誘導はなく、また公式な仕様も現時点において確認できませんでした。サンプルコードで公開している [Printer.pdc](https://github.com/cube-soft/cube.psa.samples/blob/master/Samples/Minimal/Packaging/Assets/Printer.pdc) ファイルは、Microsoft が公開している [Print Schema に関するドキュメント](https://learn.microsoft.com/ja-jp/windows/win32/printdocs/printcapabilities-public-keywords) を参考にしつつ、実際の挙動を解析した結果を基に、最低限の要素のみで作成しています。

また、PDR ファイルも同様に仕様を確認できていないのですが、こちらは省略可能であるため、現時点では無視しています。

#### アンインストールおよび更新時における注意点

現在、PSA v4 対応の仮想プリンターは MSIX インストーラーでのみインストールが可能となっていますが、アンインストール時に完全にはアンインストールされない現象を確認しています。完全にアンインストールする場合、Windows の標準機能でアンインストールを実行後、レジストリエディタで下記サブキーにある該当の仮想プリンター用ポートと予想されるサブキーを削除し、再起動して下さい。

```
HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Print\Monitors\Virtual Port Monitor\Ports
```

例えば、開発環境で Minimal プロジェクトの仮想プリンターをインストールした場合、下記のような名前のポートが作成されていました。

```
CubePsaMinimal_re2h7yn3p4en2_cube-psa-minimal:ps-printer_S-1-5-21-3114412831-3744230469-3766615254-1003
```

また、開発中に Package.appxmanifest (AppxManifest.xml) ファイルを修正した場合、アンインストールや再インストールを行っても修正内容が反映されない事があります。この場合も、いったんアンインストール後に自力で該当サブキーを削除し、再起動すると症状が改善する事があります。

## おわりに

以上が、弊社が PSA v4 仮想プリンターに関して、現時点までで解析・調査した結果となります。今回、公開した内容は、実機テストをした結果から推測したものが多分に含まれていますので、間違い等もあるかと思います。訂正や補足事項等がありましたら、[GitHub 上の本プロジェクト](https://github.com/cube-soft/cube.psa.samples) に対して、プルリクエスト (PR) 等を頂けると幸いです。

公開したものは、仕様を把握している側であれば短期間で完了できる内容であるというのが率直な感想ですが、現状では、その仕様を把握するための公式情報がほとんど提供されていません。その結果、仕様が不明・曖昧なために手探りで検証を行わざるを得ず、膨大な日数を費やす事となりました。

同様の問題に苦しんでいる開発者の助けになれば幸いです。