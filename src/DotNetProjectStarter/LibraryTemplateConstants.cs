internal static class LibraryTemplateConstants
{
    public const string LibraryTemplateCIYML = """
            name: Continuous Integration
            on:
              push:
                branches:
                  - "main"
                  - "release/*"
                paths-ignore:
                - 'docs/**'
              pull_request:
                paths-ignore:
                - 'docs/**'

            env:
              TreatWarningsAsErrors: true
              ContinuousIntegrationBuild: true
              PublishRepositoryUrl: true

            jobs:
              build:
                name: Build
                strategy:
                  fail-fast: false
                  matrix:
                    os: [ubuntu-latest, windows-latest, macos-latest]
                    configuration: [debug, release]

                runs-on: ${{ matrix.os }}

                steps:
                  - uses: actions/checkout@11bd71901bbe5b1630ceea73d27597364c9af683 # v4.2.2
                    with:
                      fetch-depth: 0

                  - name: Setup .NET
                    uses: actions/setup-dotnet@67a3573c9a986a3f9c594539f4ab511d57bb3ce9 # v4.3.1
                    with:
                      global-json-file: global.json

                  - name: Restore
                    run: dotnet restore

                  - name: Build
                    run: dotnet build --no-restore --configuration ${{ matrix.configuration }} -bl:artifacts/log/build.binlog

                  - name: Test
                    run: dotnet test --no-build --no-progress --configuration ${{ matrix.configuration }} -bl:artifacts/log/test.binlog

                  - name: Pack
                    run: dotnet pack --no-build --configuration ${{ matrix.configuration }} -bl:artifacts/log/pack.binlog

                  - name: Upload NuGet Packages
                    if: matrix.configuration == 'release' && matrix.os == 'windows-latest'
                    uses: actions/upload-artifact@330a01c490aca151604b8cf639adc76d48f6c5d4 # v5.0.0
                    with:
                      name: nuget-packages-${{ matrix.os }}_${{ matrix.configuration }}
                      path: ./artifacts/package/**/*
                      if-no-files-found: error

                  - name: Upload Build Artifacts
                    uses: actions/upload-artifact@330a01c490aca151604b8cf639adc76d48f6c5d4 # v5.0.0
                    with:
                      name: ${{ matrix.os }}_${{ matrix.configuration }}
                      path: |
                        ./artifacts/bin/**/*
                        ./artifacts/log/**/*
                      if-no-files-found: error

            """;

    public const string ReleaseYML = """
        name: Release

        on:
          push:
            tags:
              - 'v*.*.*'
          workflow_dispatch:
            inputs:
              publish-to-nuget:
                description: 'Publish to NuGet.org'
                required: true
                type: boolean
                default: false

        permissions:
          id-token: write
          contents: read

        env:
          TreatWarningsAsErrors: true
          ContinuousIntegrationBuild: true
          PublishRepositoryUrl: true

        jobs:
          build-and-pack:
            name: Build and Pack
            runs-on: windows-latest

            steps:
              - uses: actions/checkout@11bd71901bbe5b1630ceea73d27597364c9af683 # v4.2.2
                with:
                  fetch-depth: 0

              - name: Setup .NET
                uses: actions/setup-dotnet@67a3573c9a986a3f9c594539f4ab511d57bb3ce9 # v4.3.1
                with:
                  global-json-file: global.json

              - name: Restore
                run: dotnet restore

              - name: Build
                run: dotnet build --no-restore --configuration release -bl:artifacts/log/build.binlog

              - name: Test
                run: dotnet test --no-build --no-progress --configuration release -bl:artifacts/log/test.binlog

              - name: Pack
                run: dotnet pack --no-build --configuration release -bl:artifacts/log/pack.binlog

              - name: Upload NuGet Packages
                uses: actions/upload-artifact@330a01c490aca151604b8cf639adc76d48f6c5d4 # v5.0.0
                with:
                  name: nuget-packages
                  path: ./artifacts/package/release/*.nupkg
                  if-no-files-found: error

              - name: Upload Build Logs
                if: always()
                uses: actions/upload-artifact@330a01c490aca151604b8cf639adc76d48f6c5d4 # v5.0.0
                with:
                  name: build-logs
                  path: ./artifacts/log/**/*
                  if-no-files-found: error

          publish-nuget:
            name: Publish to NuGet.org
            needs: build-and-pack
            runs-on: ubuntu-latest
            if: |
              (github.event_name == 'push' && startsWith(github.ref, 'refs/tags/v')) ||
              (github.event_name == 'workflow_dispatch' && inputs.publish-to-nuget == true)

            steps:
              - uses: actions/checkout@11bd71901bbe5b1630ceea73d27597364c9af683 # v4.2.2
                with:
                  fetch-depth: 0

              - name: Setup .NET
                uses: actions/setup-dotnet@67a3573c9a986a3f9c594539f4ab511d57bb3ce9 # v4.3.1
                with:
                  global-json-file: global.json

              - name: Download NuGet Packages
                uses: actions/download-artifact@018cc2cf5baa6db3ef3c5f8a56943fffe632ef53 # v6.0.0
                with:
                  name: nuget-packages
                  path: ./packages

              - name: Authenticate to nuget
                uses: NuGet/login@d22cc5f58ff5b88bf9bd452535b4335137e24544 # v1.1.0
                id: nugetlogin
                with:
                  user: {0}

              - name: Push to NuGet.org
                run: dotnet nuget push "./packages/**/*.nupkg" --api-key "${{ steps.nugetlogin.outputs.NUGET_API_KEY }}" --source https://api.nuget.org/v3/index.json --skip-duplicate

          create-github-release:
            name: Create GitHub Release
            needs: build-and-pack
            runs-on: ubuntu-latest
            if: github.event_name == 'push' && startsWith(github.ref, 'refs/tags/v')
            permissions:
              contents: write

            steps:
              - uses: actions/checkout@11bd71901bbe5b1630ceea73d27597364c9af683 # v4.2.2

              - name: Download NuGet Packages
                uses: actions/download-artifact@018cc2cf5baa6db3ef3c5f8a56943fffe632ef53 # v6.0.0
                with:
                  name: nuget-packages
                  path: ./packages

              - name: Create Release
                uses: softprops/action-gh-release@c062e08bd532815e2082a85e87e3ef29c3e6d191 # v2.2.1
                with:
                  files: ./packages/*.nupkg
                  generate_release_notes: true
                  draft: false
                  prerelease: ${{ contains(github.ref, '-') }}

        """;

    public const string ProjectFile = """
        ﻿<Project Sdk="Microsoft.NET.Sdk">

          <PropertyGroup>
            <TargetFrameworks>net462;net8.0</TargetFrameworks>
          </PropertyGroup>

        </Project>

        """;

    public const string ProjectFileWithExplicitPackageId = """
        ﻿<Project Sdk="Microsoft.NET.Sdk">

          <PropertyGroup>
            <TargetFrameworks>net462;net8.0</TargetFrameworks>
            <PackageId>{0}</PackageId>
          </PropertyGroup>

        </Project>

        """;

    public const string TestDirectoryBuildProps = """
        <Project>

          <Import Condition="Exists($([MSBuild]::GetPathOfFileAbove('Directory.Build.props', '$(MSBuildThisFileDirectory)../')))" Project="$([MSBuild]::GetPathOfFileAbove('Directory.Build.props', '$(MSBuildThisFileDirectory)../'))" />

          <PropertyGroup>
            <NoWarn>$(NoWarn);CS1591</NoWarn>
          </PropertyGroup>

        </Project>

        """;

    public const string TestProjectFile = """
        <Project Sdk="MSTest.Sdk">

          <PropertyGroup>
            <TargetFramework>net10.0</TargetFramework>
          </PropertyGroup>

        </Project>

        """;

    public const string TestProjectAssemblyInfoFile = """
        using Microsoft.VisualStudio.TestTools.UnitTesting;

        [assembly: Parallelize(Scope = ExecutionScope.MethodLevel, Workers = 0)]

        """;

    public const string EditorConfigFile = """
        # top-most EditorConfig file
        root = true

        # Don't use tabs for indentation.
        [*]
        indent_style = space

        # Visual Studio XML project files
        [*.{csproj,vbproj,vcxproj,proj,projitems,shproj,props,targets,slnx}]
        indent_size = 2
        charset = utf-8-bom

        [*.{cs,vb}]
        indent_size = 4
        insert_final_newline = true
        charset = utf-8-bom

        """;

    public const string GitAttributesFile = """
        ## Set Git attributes for paths including line ending
        ## normalization, diff behavior, etc.
        ##
        ## Get latest from `dotnet new gitattributes`

        # Auto detect text files and perform LF normalization
        * text=auto

        #
        # The above will handle all files NOT found below
        #

        *.cs     text diff=csharp
        *.cshtml text diff=html
        *.csx    text diff=csharp
        *.sln    text eol=crlf

        # Content below from: https://github.com/gitattributes/gitattributes/blob/master/Common.gitattributes

        # Documents
        *.bibtex   text diff=bibtex
        *.doc      diff=astextplain
        *.DOC      diff=astextplain
        *.docx     diff=astextplain
        *.DOCX     diff=astextplain
        *.dot      diff=astextplain
        *.DOT      diff=astextplain
        *.pdf      diff=astextplain
        *.PDF      diff=astextplain
        *.rtf      diff=astextplain
        *.RTF      diff=astextplain
        *.md       text diff=markdown
        *.mdx      text diff=markdown
        *.tex      text diff=tex
        *.adoc     text
        *.textile  text
        *.mustache text
        # Per RFC 4180, .csv should be CRLF
        *.csv      text eol=crlf
        *.tab      text
        *.tsv      text
        *.txt      text
        *.sql      text
        *.epub     diff=astextplain

        # Graphics
        *.png      binary
        *.jpg      binary
        *.jpeg     binary
        *.gif      binary
        *.tif      binary
        *.tiff     binary
        *.ico      binary
        # SVG treated as text by default.
        *.svg      text
        # If you want to treat it as binary,
        # use the following line instead.
        # *.svg    binary
        *.eps      binary

        # Scripts
        # Force Unix scripts to always use lf line endings so that if a repo is accessed
        # in Unix via a file share from Windows, the scripts will work
        *.bash     text eol=lf
        *.fish     text eol=lf
        *.ksh      text eol=lf
        *.sh       text eol=lf
        *.zsh      text eol=lf
        # Likewise, force cmd and batch scripts to always use crlf
        *.bat      text eol=crlf
        *.cmd      text eol=crlf

        # Serialization
        *.json     text
        *.toml     text
        *.xml      text
        *.yaml     text
        *.yml      text

        # Archives
        *.7z       binary
        *.bz       binary
        *.bz2      binary
        *.bzip2    binary
        *.gz       binary
        *.lz       binary
        *.lzma     binary
        *.rar      binary
        *.tar      binary
        *.taz      binary
        *.tbz      binary
        *.tbz2     binary
        *.tgz      binary
        *.tlz      binary
        *.txz      binary
        *.xz       binary
        *.Z        binary
        *.zip      binary
        *.zst      binary

        # Text files where line endings should be preserved
        *.patch    -text

        # Exclude files from exporting
        .gitattributes export-ignore
        .gitignore     export-ignore
        .gitkeep       export-ignore

        """;

    public const string GitIgnoreFile = """
        ## Ignore Visual Studio temporary files, build results, and
        ## files generated by popular Visual Studio add-ons.
        ##
        ## Get latest from `dotnet new gitignore`

        # dotenv files
        .env

        # User-specific files
        *.rsuser
        *.suo
        *.user
        *.userosscache
        *.sln.docstates

        # User-specific files (MonoDevelop/Xamarin Studio)
        *.userprefs

        # Mono auto generated files
        mono_crash.*

        # Build results
        [Dd]ebug/
        [Dd]ebugPublic/
        [Rr]elease/
        [Rr]eleases/
        x64/
        x86/
        [Ww][Ii][Nn]32/
        [Aa][Rr][Mm]/
        [Aa][Rr][Mm]64/
        bld/
        [Bb]in/
        [Oo]bj/
        [Ll]og/
        [Ll]ogs/

        # Visual Studio 2015/2017 cache/options directory
        .vs/
        # Uncomment if you have tasks that create the project's static files in wwwroot
        #wwwroot/

        # Visual Studio 2017 auto generated files
        Generated\ Files/

        # MSTest test Results
        [Tt]est[Rr]esult*/
        [Bb]uild[Ll]og.*

        # NUnit
        *.VisualState.xml
        TestResult.xml
        nunit-*.xml

        # Build Results of an ATL Project
        [Dd]ebugPS/
        [Rr]eleasePS/
        dlldata.c

        # Benchmark Results
        BenchmarkDotNet.Artifacts/

        # .NET
        project.lock.json
        project.fragment.lock.json
        artifacts/

        # Tye
        .tye/

        # ASP.NET Scaffolding
        ScaffoldingReadMe.txt

        # StyleCop
        StyleCopReport.xml

        # Files built by Visual Studio
        *_i.c
        *_p.c
        *_h.h
        *.ilk
        *.meta
        *.obj
        *.iobj
        *.pch
        *.pdb
        *.ipdb
        *.pgc
        *.pgd
        *.rsp
        *.sbr
        *.tlb
        *.tli
        *.tlh
        *.tmp
        *.tmp_proj
        *_wpftmp.csproj
        *.log
        *.tlog
        *.vspscc
        *.vssscc
        .builds
        *.pidb
        *.svclog
        *.scc

        # Chutzpah Test files
        _Chutzpah*

        # Visual C++ cache files
        ipch/
        *.aps
        *.ncb
        *.opendb
        *.opensdf
        *.sdf
        *.cachefile
        *.VC.db
        *.VC.VC.opendb

        # Visual Studio profiler
        *.psess
        *.vsp
        *.vspx
        *.sap

        # Visual Studio Trace Files
        *.e2e

        # TFS 2012 Local Workspace
        $tf/

        # Guidance Automation Toolkit
        *.gpState

        # ReSharper is a .NET coding add-in
        _ReSharper*/
        *.[Rr]e[Ss]harper
        *.DotSettings.user

        # TeamCity is a build add-in
        _TeamCity*

        # DotCover is a Code Coverage Tool
        *.dotCover

        # AxoCover is a Code Coverage Tool
        .axoCover/*
        !.axoCover/settings.json

        # Coverlet is a free, cross platform Code Coverage Tool
        coverage*.json
        coverage*.xml
        coverage*.info

        # Visual Studio code coverage results
        *.coverage
        *.coveragexml

        # NCrunch
        _NCrunch_*
        .*crunch*.local.xml
        nCrunchTemp_*

        # MightyMoose
        *.mm.*
        AutoTest.Net/

        # Web workbench (sass)
        .sass-cache/

        # Installshield output folder
        [Ee]xpress/

        # DocProject is a documentation generator add-in
        DocProject/buildhelp/
        DocProject/Help/*.HxT
        DocProject/Help/*.HxC
        DocProject/Help/*.hhc
        DocProject/Help/*.hhk
        DocProject/Help/*.hhp
        DocProject/Help/Html2
        DocProject/Help/html

        # Click-Once directory
        publish/

        # Publish Web Output
        *.[Pp]ublish.xml
        *.azurePubxml
        # Note: Comment the next line if you want to checkin your web deploy settings,
        # but database connection strings (with potential passwords) will be unencrypted
        *.pubxml
        *.publishproj

        # Microsoft Azure Web App publish settings. Comment the next line if you want to
        # checkin your Azure Web App publish settings, but sensitive information contained
        # in these scripts will be unencrypted
        PublishScripts/

        # NuGet Packages
        *.nupkg
        # NuGet Symbol Packages
        *.snupkg
        # The packages folder can be ignored because of Package Restore
        **/[Pp]ackages/*
        # except build/, which is used as an MSBuild target.
        !**/[Pp]ackages/build/
        # Uncomment if necessary however generally it will be regenerated when needed
        #!**/[Pp]ackages/repositories.config
        # NuGet v3's project.json files produces more ignorable files
        *.nuget.props
        *.nuget.targets

        # Microsoft Azure Build Output
        csx/
        *.build.csdef

        # Microsoft Azure Emulator
        ecf/
        rcf/

        # Windows Store app package directories and files
        AppPackages/
        BundleArtifacts/
        Package.StoreAssociation.xml
        _pkginfo.txt
        *.appx
        *.appxbundle
        *.appxupload

        # Visual Studio cache files
        # files ending in .cache can be ignored
        *.[Cc]ache
        # but keep track of directories ending in .cache
        !?*.[Cc]ache/

        # Others
        ClientBin/
        ~$*
        *~
        *.dbmdl
        *.dbproj.schemaview
        *.jfm
        *.pfx
        *.publishsettings
        orleans.codegen.cs

        # Since there are multiple workflows, uncomment next line to ignore bower_components
        # (https://github.com/github/gitignore/pull/1529#issuecomment-104372622)
        #bower_components/

        # RIA/Silverlight projects
        Generated_Code/

        # Backup & report files from converting an old project file
        # to a newer Visual Studio version. Backup files are not needed,
        # because we have git ;-)
        _UpgradeReport_Files/
        Backup*/
        UpgradeLog*.XML
        UpgradeLog*.htm
        ServiceFabricBackup/
        *.rptproj.bak

        # SQL Server files
        *.mdf
        *.ldf
        *.ndf

        # Business Intelligence projects
        *.rdl.data
        *.bim.layout
        *.bim_*.settings
        *.rptproj.rsuser
        *- [Bb]ackup.rdl
        *- [Bb]ackup ([0-9]).rdl
        *- [Bb]ackup ([0-9][0-9]).rdl

        # Microsoft Fakes
        FakesAssemblies/

        # GhostDoc plugin setting file
        *.GhostDoc.xml

        # Node.js Tools for Visual Studio
        .ntvs_analysis.dat
        node_modules/

        # Visual Studio 6 build log
        *.plg

        # Visual Studio 6 workspace options file
        *.opt

        # Visual Studio 6 auto-generated workspace file (contains which files were open etc.)
        *.vbw

        # Visual Studio 6 auto-generated project file (contains which files were open etc.)
        *.vbp

        # Visual Studio 6 workspace and project file (working project files containing files to include in project)
        *.dsw
        *.dsp

        # Visual Studio 6 technical files
        *.ncb
        *.aps

        # Visual Studio LightSwitch build output
        **/*.HTMLClient/GeneratedArtifacts
        **/*.DesktopClient/GeneratedArtifacts
        **/*.DesktopClient/ModelManifest.xml
        **/*.Server/GeneratedArtifacts
        **/*.Server/ModelManifest.xml
        _Pvt_Extensions

        # Paket dependency manager
        .paket/paket.exe
        paket-files/

        # FAKE - F# Make
        .fake/

        # CodeRush personal settings
        .cr/personal

        # Python Tools for Visual Studio (PTVS)
        __pycache__/
        *.pyc

        # Cake - Uncomment if you are using it
        # tools/**
        # !tools/packages.config

        # Tabs Studio
        *.tss

        # Telerik's JustMock configuration file
        *.jmconfig

        # BizTalk build output
        *.btp.cs
        *.btm.cs
        *.odx.cs
        *.xsd.cs

        # OpenCover UI analysis results
        OpenCover/

        # Azure Stream Analytics local run output
        ASALocalRun/

        # MSBuild Binary and Structured Log
        *.binlog

        # NVidia Nsight GPU debugger configuration file
        *.nvuser

        # MFractors (Xamarin productivity tool) working folder
        .mfractor/

        # Local History for Visual Studio
        .localhistory/

        # Visual Studio History (VSHistory) files
        .vshistory/

        # BeatPulse healthcheck temp database
        healthchecksdb

        # Backup folder for Package Reference Convert tool in Visual Studio 2017
        MigrationBackup/

        # Ionide (cross platform F# VS Code tools) working folder
        .ionide/

        # Fody - auto-generated XML schema
        FodyWeavers.xsd

        # VS Code files for those working on multiple tools
        .vscode/*
        !.vscode/settings.json
        !.vscode/tasks.json
        !.vscode/launch.json
        !.vscode/extensions.json
        *.code-workspace

        # Local History for Visual Studio Code
        .history/

        # Windows Installer files from build outputs
        *.cab
        *.msi
        *.msix
        *.msm
        *.msp

        # JetBrains Rider
        *.sln.iml
        .idea/

        ##
        ## Visual studio for Mac
        ##


        # globs
        Makefile.in
        *.userprefs
        *.usertasks
        config.make
        config.status
        aclocal.m4
        install-sh
        autom4te.cache/
        *.tar.gz
        tarballs/
        test-results/

        # Mac bundle stuff
        *.dmg
        *.app

        # content below from: https://github.com/github/gitignore/blob/main/Global/macOS.gitignore
        # General
        .DS_Store
        .AppleDouble
        .LSOverride

        # Icon must end with two \r
        Icon


        # Thumbnails
        ._*

        # Files that might appear in the root of a volume
        .DocumentRevisions-V100
        .fseventsd
        .Spotlight-V100
        .TemporaryItems
        .Trashes
        .VolumeIcon.icns
        .com.apple.timemachine.donotpresent

        # Directories potentially created on remote AFP share
        .AppleDB
        .AppleDesktop
        Network Trash Folder
        Temporary Items
        .apdisk

        # content below from: https://github.com/github/gitignore/blob/main/Global/Windows.gitignore
        # Windows thumbnail cache files
        Thumbs.db
        ehthumbs.db
        ehthumbs_vista.db

        # Dump file
        *.stackdump

        # Folder config file
        [Dd]esktop.ini

        # Recycle Bin used on file shares
        $RECYCLE.BIN/

        # Windows Installer files
        *.cab
        *.msi
        *.msix
        *.msm
        *.msp

        # Windows shortcuts
        *.lnk

        # Vim temporary swap files
        *.swp

        """;

    public const string GlobalConfigFile = """
        # IDE0001: Simplify name (IDE-only per docs)
        # IDE0002: Simplify member access (IDE-only per docs)
        
        # IDE0003 and IDE0009: this and Me preferences (option defaults are already false) (IDE-only per docs)
        dotnet_style_qualification_for_field = false
        dotnet_style_qualification_for_property = false
        dotnet_style_qualification_for_method = false
        dotnet_style_qualification_for_event = false
        dotnet_diagnostic.IDE0003.severity = suggestion
        
        # IDE0004: Remove unnecessary cast
        dotnet_diagnostic.IDE0004.severity = warning
        
        # IDE0005: Remove unnecessary imports
        dotnet_diagnostic.IDE0005.severity = warning
        
        # IDE0007 and IDE0008: var preferences
        dotnet_diagnostic.IDE0007.severity = none
        dotnet_diagnostic.IDE0008.severity = none
        
        # IDE0010: Add missing cases to switch statement
        dotnet_diagnostic.IDE0010.severity = warning
        
        # IDE0011: Add braces
        dotnet_diagnostic.IDE0011.severity = none
        
        # IDE0016: Use throw expression
        dotnet_diagnostic.IDE0016.severity = warning
        
        # IDE0017: Use object initializers
        dotnet_style_object_initializer = true
        dotnet_diagnostic.IDE0017.severity = warning
        
        # IDE0018: Inline variable declaration
        dotnet_diagnostic.IDE0018.severity = warning
        
        # IDE0019: Use pattern matching to avoid 'as' followed by a 'null' check
        # IDE0020 and IDE0038: Use pattern matching to avoid 'is' check followed by a cast
        # IDE0078 and IDE0260: Use pattern matching
        # IDE0083: Use pattern matching (not operator)
        # IDE0084: Use pattern matching (IsNot operator) (VB-only)
        
        csharp_style_pattern_matching_over_is_with_cast_check = true
        csharp_style_pattern_matching_over_as_with_null_check = true
        dotnet_diagnostic.IDE0019.severity = warning
        dotnet_diagnostic.IDE0020.severity = warning
        dotnet_diagnostic.IDE0038.severity = warning
        dotnet_diagnostic.IDE0078.severity = warning
        dotnet_diagnostic.IDE0083.severity = warning
        dotnet_diagnostic.IDE0260.severity = warning
        
        # IDE0021: Use expression body for constructors
        # IDE0022: Use expression body for methods
        # IDE0023 and IDE0024: Use expression body for operators
        # IDE0025: Use expression body for properties
        # IDE0026: Use expression body for indexers
        # IDE0027: Use expression body for accessors
        # IDE0053: Use expression body for lambdas
        # IDE0061: Use expression body for local functions
        csharp_style_expression_bodied_methods = true
        csharp_style_expression_bodied_constructors = true
        csharp_style_expression_bodied_operators = true
        csharp_style_expression_bodied_properties = true
        csharp_style_expression_bodied_indexers = true
        csharp_style_expression_bodied_accessors = true
        csharp_style_expression_bodied_lambdas = true
        csharp_style_expression_bodied_local_functions = true
        dotnet_diagnostic.IDE0021.severity = warning
        dotnet_diagnostic.IDE0022.severity = warning
        dotnet_diagnostic.IDE0023.severity = warning
        dotnet_diagnostic.IDE0024.severity = warning
        dotnet_diagnostic.IDE0025.severity = warning
        dotnet_diagnostic.IDE0026.severity = warning
        dotnet_diagnostic.IDE0027.severity = warning
        dotnet_diagnostic.IDE0053.severity = warning
        dotnet_diagnostic.IDE0061.severity = warning
        
        # IDE0028: Use collection initializers or expressions
        # IDE0300: Use collection expression for array
        # IDE0301: Use collection expression for empty
        # IDE0302: Use collection expression for stackalloc
        # IDE0303: Use collection expression for Create()
        # IDE0304: Use collection expression for builder
        # IDE0305: Use collection expression for fluent
        # IDE0306: Use collection expression for new
        dotnet_style_collection_initializer = true
        dotnet_style_prefer_collection_expression = when_types_exactly_match
        dotnet_diagnostic.IDE0028.severity = none
        dotnet_diagnostic.IDE0300.severity = none
        dotnet_diagnostic.IDE0301.severity = none
        dotnet_diagnostic.IDE0302.severity = none
        dotnet_diagnostic.IDE0303.severity = none
        dotnet_diagnostic.IDE0304.severity = none
        dotnet_diagnostic.IDE0305.severity = none
        dotnet_diagnostic.IDE0306.severity = none
        
        # IDE0029: Null check can be simplified (ternary conditional check)
        # IDE0030: Null check can be simplified (nullable ternary conditional check)
        # IDE0270: Null check can be simplified (if null check)
        dotnet_style_coalesce_expression = true
        dotnet_diagnostic.IDE0029.severity = warning
        dotnet_diagnostic.IDE0030.severity = warning
        dotnet_diagnostic.IDE0270.severity = warning
        
        # IDE0031: Use null propagation
        dotnet_style_null_propagation = true
        dotnet_diagnostic.IDE0031.severity = warning
        
        # IDE0032: Use auto-implemented property
        dotnet_style_prefer_auto_properties = true
        dotnet_diagnostic.IDE0032.severity = warning
        
        # IDE0033: Use explicitly provided tuple name
        dotnet_style_explicit_tuple_names = true
        dotnet_diagnostic.IDE0033.severity = warning
        
        # IDE0034: Simplify 'default' expression
        csharp_prefer_simple_default_expression = true
        dotnet_diagnostic.IDE0034.severity = warning
        
        # IDE0035: Remove unreachable code (IDE-only per docs)
        dotnet_diagnostic.IDE0035.severity = none
        
        # IDE0036: Order modifiers
        # IDE0040: Add accessibility modifiers
        csharp_preferred_modifier_order = public,private,protected,internal,file,static,extern,new,virtual,abstract,sealed,override,readonly,unsafe,required,volatile,async
        dotnet_diagnostic.IDE0036.severity = warning
        dotnet_diagnostic.IDE0040.severity = warning
        
        # IDE0037: Use inferred member names
        dotnet_style_prefer_inferred_tuple_names = true
        dotnet_style_prefer_inferred_anonymous_type_member_names = true
        dotnet_diagnostic.IDE0037.severity = warning
        
        # IDE0039: Use local function instead of lambda
        dotnet_diagnostic.IDE0039.severity = none
        
        # IDE0040: Add accessibility modifiers
        dotnet_style_require_accessibility_modifiers = for_non_interface_members
        dotnet_diagnostic.IDE0040.severity = warning
        
        # IDE0041: Use 'is null' check
        dotnet_style_prefer_is_null_check_over_reference_equality_method = true
        dotnet_diagnostic.IDE0041.severity = warning
        
        # IDE0042: Deconstruct variable declaration
        dotnet_diagnostic.IDE0042.severity = warning
        
        # IDE0044: Add readonly modifier
        dotnet_style_readonly_field = true
        dotnet_diagnostic.IDE0044.severity = warning
        
        # IDE0045: Use conditional expression for assignment
        dotnet_style_prefer_conditional_expression_over_assignment = true
        dotnet_diagnostic.IDE0045.severity = none
        
        # IDE0046: Use conditional expression for return
        dotnet_style_prefer_conditional_expression_over_return = true
        dotnet_diagnostic.IDE0046.severity = none
        
        # IDE0047 and IDE0048: Parentheses preferences
        dotnet_diagnostic.IDE0047.severity = warning
        dotnet_diagnostic.IDE0048.severity = warning
        
        # IDE0049: Use language keywords instead of framework type names for type references
        # Default is already true. This rule is not enabled on build.
        dotnet_style_predefined_type_for_locals_parameters_members = true
        dotnet_style_predefined_type_for_member_access = true
        dotnet_diagnostic.IDE0049.severity = warning
        
        # IDE0051: Remove unused private member
        dotnet_diagnostic.IDE0051.severity = warning
        
        # IDE0052: Remove unread private member
        dotnet_diagnostic.IDE0052.severity = warning
        
        # IDE0054 and IDE0074: Use compound assignment
        dotnet_diagnostic.IDE0054.severity = warning
        dotnet_diagnostic.IDE0074.severity = warning
        
        # IDE0055: Fix formatting
        dotnet_diagnostic.IDE0055.severity = warning
        
        # .NET formatting options - IDE0055
        dotnet_sort_system_directives_first = true
        dotnet_separate_import_directive_groups = false
        
        # New line preferences - IDE0055
        csharp_new_line_before_open_brace = all
        csharp_new_line_before_else = true
        csharp_new_line_before_catch = true
        csharp_new_line_before_finally = true
        csharp_new_line_before_members_in_object_initializers = true
        csharp_new_line_before_members_in_anonymous_types = true
        csharp_new_line_between_query_expression_clauses = true
        
        # Indentation preferences - IDE0055
        csharp_indent_block_contents = true
        csharp_indent_braces = false
        csharp_indent_case_contents = true
        csharp_indent_case_contents_when_block = false
        csharp_indent_switch_labels = true
        csharp_indent_labels = one_less_than_current
        
        # Spacing preferences - IDE0055
        csharp_space_after_cast = false
        csharp_space_after_colon_in_inheritance_clause = true
        csharp_space_after_comma  = true
        csharp_space_after_dot = false
        csharp_space_after_keywords_in_control_flow_statements = true
        csharp_space_after_semicolon_in_for_statement = true
        csharp_space_around_binary_operators = before_and_after
        csharp_space_around_declaration_statements = do_not_ignore
        csharp_space_before_colon_in_inheritance_clause = true
        csharp_space_before_comma = false
        csharp_space_before_dot = false
        csharp_space_before_open_square_brackets = false
        csharp_space_before_semicolon_in_for_statement = false
        csharp_space_between_empty_square_brackets = false
        csharp_space_between_method_call_empty_parameter_list_parentheses = false
        csharp_space_between_method_call_name_and_opening_parenthesis = false
        csharp_space_between_method_call_parameter_list_parentheses = false
        csharp_space_between_method_declaration_empty_parameter_list_parentheses = false
        csharp_space_between_method_declaration_name_and_open_parenthesis = false
        csharp_space_between_method_declaration_parameter_list_parentheses = false
        csharp_space_between_parentheses = false
        csharp_space_between_square_brackets = false
        
        # Wrapping preferences - IDE0055
        csharp_preserve_single_line_blocks = true
        csharp_preserve_single_line_statements = false
        
        # IDE0056: Use index operator
        csharp_style_prefer_index_operator = false
        dotnet_diagnostic.IDE0056.severity = warning
        
        # IDE0057: Use range operator
        csharp_style_prefer_range_operator = false
        dotnet_diagnostic.IDE0057.severity = warning
        
        # IDE0058: Remove unnecessary expression value
        dotnet_diagnostic.IDE0058.severity = warning
        
        # IDE0059: Remove unnecessary value assignment
        dotnet_diagnostic.IDE0059.severity = warning
        
        # IDE0060: Remove unused parameter
        dotnet_diagnostic.IDE0060.severity = warning
        
        # IDE0062: Make local function static
        csharp_prefer_static_local_function = true
        dotnet_diagnostic.IDE0062.severity = warning
        
        # IDE0063: Use simple 'using' statement
        csharp_prefer_simple_using_statement = true
        dotnet_diagnostic.IDE0063.severity = warning
        
        # IDE0064: Make struct fields writable
        dotnet_diagnostic.IDE0064.severity = warning
        
        # IDE0065: 'using' directive placement
        dotnet_diagnostic.IDE0065.severity = none
        
        # IDE0066: Use switch expression
        csharp_style_prefer_switch_expression = true
        dotnet_diagnostic.IDE0066.severity = warning
        
        # IDE0070: Use 'System.HashCode.Combine'
        dotnet_diagnostic.IDE0070.severity = warning
        
        # IDE0071: Simplify interpolation
        dotnet_diagnostic.IDE0071.severity = warning
        
        # IDE0072: Add missing cases to switch expression
        dotnet_diagnostic.IDE0072.severity = warning
        
        # IDE0073: Require file header
        dotnet_diagnostic.IDE0073.severity = none
        
        # IDE0075: Simplify conditional expression
        dotnet_diagnostic.IDE0075.severity = warning
        
        # IDE0076: Remove invalid global 'SuppressMessageAttribute'
        dotnet_diagnostic.IDE0076.severity = warning
        
        # IDE0077: Avoid legacy format target in global 'SuppressMessageAttribute'
        dotnet_diagnostic.IDE0077.severity = warning
        
        # IDE0079: Remove unnecessary suppression (IDE-only per docs)
        dotnet_diagnostic.IDE0079.severity = none
        
        # IDE0080: Remove unnecessary suppression operator
        dotnet_diagnostic.IDE0080.severity = warning
        
        # IDE0081: Remove ByVal (VB-only)
        dotnet_diagnostic.IDE0081.severity = warning
        
        # IDE0082: Convert typeof to nameof
        dotnet_diagnostic.IDE0082.severity = warning
        
        # IDE0090: Simplify new expression
        dotnet_diagnostic.IDE0090.severity = none
        
        # IDE0100: Remove unnecessary equality operator
        dotnet_diagnostic.IDE0100.severity = warning
        
        # IDE0160 and IDE0161
        csharp_style_namespace_declarations = file_scoped
        dotnet_diagnostic.IDE0160.severity = warning
        dotnet_diagnostic.IDE0161.severity = warning
        
        # IDE1006: Naming rule violation
        dotnet_diagnostic.IDE1006.severity = warning
        
        # Naming rules: name all constant fields using PascalCase (IDE1006)
        dotnet_naming_rule.constant_fields_should_be_pascal_case.severity = warning
        dotnet_naming_rule.constant_fields_should_be_pascal_case.symbols  = constant_fields
        dotnet_naming_rule.constant_fields_should_be_pascal_case.style    = pascal_case_style
        dotnet_naming_symbols.constant_fields.applicable_kinds   = field
        dotnet_naming_symbols.constant_fields.required_modifiers = const
        dotnet_naming_style.pascal_case_style.capitalization = pascal_case
        
        # Naming rules: static fields should have s_ prefix (IDE1006)
        dotnet_naming_rule.static_fields_should_have_prefix.severity = suggestion
        dotnet_naming_rule.static_fields_should_have_prefix.symbols  = static_fields
        dotnet_naming_rule.static_fields_should_have_prefix.style    = static_prefix_style
        dotnet_naming_symbols.static_fields.applicable_kinds   = field
        dotnet_naming_symbols.static_fields.required_modifiers = static
        dotnet_naming_symbols.static_fields.applicable_accessibilities = private, internal, private_protected
        dotnet_naming_style.static_prefix_style.required_prefix = s_
        dotnet_naming_style.static_prefix_style.capitalization = camel_case
        
        # Naming rules: internal and private fields should be _camelCase (IDE1006)
        dotnet_naming_rule.camel_case_for_private_internal_fields.severity = suggestion
        dotnet_naming_rule.camel_case_for_private_internal_fields.symbols  = private_internal_fields
        dotnet_naming_rule.camel_case_for_private_internal_fields.style    = camel_case_underscore_style
        dotnet_naming_symbols.private_internal_fields.applicable_kinds = field
        dotnet_naming_symbols.private_internal_fields.applicable_accessibilities = private, internal
        dotnet_naming_style.camel_case_underscore_style.required_prefix = _
        dotnet_naming_style.camel_case_underscore_style.capitalization = camel_case
        
        # IDE1005
        csharp_style_conditional_delegate_call = true
        
        # CA1014: Mark assemblies with CLSCompliant
        dotnet_diagnostic.CA1014.severity = none
        
        # CA1824: Mark assemblies with NeutralResourcesLanguageAttribute
        dotnet_diagnostic.CA1824.severity = none
        
        # CA1062: Validate arguments of public methods
        dotnet_diagnostic.CA1062.severity = none
        
        # CA1510: Use ArgumentNullException throw helper
        dotnet_diagnostic.CA1510.severity = none
        
        # CA1863: Use 'CompositeFormat'
        dotnet_diagnostic.CA1863.severity = none
        
        # TODO: Use YAnalyzers for var vs explicit types.

        """;

    public const string DirectoryBuildPropsFile = """
        <Project>

          <PropertyGroup>
            <UseArtifactsOutput>true</UseArtifactsOutput>
            <ArtifactsPath>$(MSBuildThisFileDirectory)artifacts</ArtifactsPath>
            <AnalysisLevel>latest-Recommended</AnalysisLevel>
            <LangVersion>preview</LangVersion>
            <Nullable>enable</Nullable>
            <EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>
            <EnableNETAnalyzers>true</EnableNETAnalyzers>
            <GenerateDocumentationFile>true</GenerateDocumentationFile>
            <SuppressNETCoreSdkPreviewMessage>true</SuppressNETCoreSdkPreviewMessage>
            <DebugType>embedded</DebugType>
            <AssemblyOriginatorKeyFile Condition="Exists('$(MSBuildThisFileDirectory)key.snk')">$(MSBuildThisFileDirectory)key.snk</AssemblyOriginatorKeyFile>
            <SignAssembly Condition="$(AssemblyOriginatorKeyFile) != ''">true</SignAssembly>
            <PackageReadmeFile>README.md</PackageReadmeFile>

            <!-- By default, NuGet doesn't include portable PDBs in nupkg. -->
            <!-- As I'm not producing a separate symbol package (*.snupkg), I want portable PDBs to be included. -->
            <!-- For more information about symbol packages, see https://learn.microsoft.com/en-us/nuget/create-packages/symbol-packages-snupkg -->
            <AllowedOutputExtensionsInPackageBuildOutputFolder>$(AllowedOutputExtensionsInPackageBuildOutputFolder);.pdb</AllowedOutputExtensionsInPackageBuildOutputFolder>

            <EmbedUntrackedSources>true</EmbedUntrackedSources>

            <Version>1.0.0-beta.1</Version>
            <Authors>{0}</Authors>
            <PackageTags>dotnet csharp</PackageTags>
            <Description>ADD_PACKAGE_DESCRIPTION_HERE</Description>
            <PackageLicenseExpression>MIT</PackageLicenseExpression>
          </PropertyGroup>

          <ItemGroup>
            <GlobalPackageReference Include="PolySharp" Version="1.15.0" />
          </ItemGroup>

          <ItemGroup>
            <None Include="$(MSBuildThisFileDirectory)README.md" Pack="true" PackagePath="\" Visible="false" />
          </ItemGroup>

        </Project>

        """;

    public const string DirectoryPackagesPropsFile = """
        <Project>

          <PropertyGroup>
            <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
          </PropertyGroup>

          <ItemGroup>
            <!-- Add PackageVersion elements here. -->
          </ItemGroup>

        </Project>

        """;

    public const string LicenseFile = """
        The MIT License (MIT)

        Copyright (c) {0}

        All rights reserved.

        Permission is hereby granted, free of charge, to any person obtaining a copy
        of this software and associated documentation files (the "Software"), to deal
        in the Software without restriction, including without limitation the rights
        to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
        copies of the Software, and to permit persons to whom the Software is
        furnished to do so, subject to the following conditions:

        The above copyright notice and this permission notice shall be included in all
        copies or substantial portions of the Software.

        THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
        IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
        FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
        AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
        LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
        OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
        SOFTWARE.
        """;

    public const string ReadmeFile = """
        # {0}

        TODO

        """;

    public const string SlnxFile = """
        <Solution>
          <Configurations>
            <Platform Name="Any CPU" />
            <Platform Name="x64" />
            <Platform Name="x86" />
          </Configurations>
          <Folder Name="/Solution Items/">
            <File Path=".editorconfig" />
            <File Path=".gitignore" />
            <File Path=".globalconfig" />
            <File Path="Directory.Build.props" />
            <File Path="Directory.Build.targets" />
            <File Path="Directory.Packages.props" />
            <File Path="global.json" />
            <File Path="nuget.config" />
            <File Path="README.md" />
          </Folder>
          <Folder Name="/src/">
            <Project Path="src/{0}/{0}.csproj" />
          </Folder>
          <Folder Name="/tests/">
            <Project Path="tests/{0}.Tests/{0}.Tests.csproj" />
          </Folder>
        </Solution>
        """;

    public const string GlobalJsonFile = """
        {
          "sdk": {
            "version": "10.0.101"
          },
          "test": {
            "runner": "Microsoft.Testing.Platform"
          },
          "msbuild-sdks": {
            "MSTest.Sdk": "4.0.2"
          }
        }

        """;

    public const string NuGetConfigFile = """
        <?xml version="1.0" encoding="utf-8"?>
        <configuration>
          <packageSources>
            <clear />
            <add key="nuget" value="https://api.nuget.org/v3/index.json" />
          </packageSources>
        </configuration>
        """;
}
