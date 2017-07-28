# Solution Layout

We suggest the following logical naming pattern when laying out your solution:


* image of visual studio layout video or animated gif of the side-waffle
* extension that creates everything automatically.

All application logic is stored within a core portable class library which is shared between - and referenced by - each specific platform application:

# Core Application Library

The portable class library will be the heart of your application and where you will spend most of your time. When you create the project, you will need to select a profile. You should use `Profile78` (if targeting WP8.x), `Profile259` (if targeting only WPA + Xamarin) or `Profile111` (if targeting UWP + Xamarin). Alternatively you can adjust the profile after creation by editing the `.csproj`, but you will need to run some NuGet commands to reinstall most of your packages.

MyCoolApp.Core.csproj:

```xml
<TargetFrameworkProfile>Profile78</TargetFrameworkProfile>
<TargetFrameworkVersion>v4.5</TargetFrameworkVersion>
```

MyCoolApp.Core.csproj:

```xml
<TargetFrameworkProfile>Profile259</TargetFrameworkProfile>
<TargetFrameworkVersion>v4.5</TargetFrameworkVersion>
```

MyCoolApp.Core.csproj:

```xml
<TargetFrameworkProfile>Profile111</TargetFrameworkProfile>
<TargetFrameworkVersion>v4.5</TargetFrameworkVersion>
```

# Core Application Tests

A standard class library that contains unit tests that confirm functionality
of .Core.

# Target Platform Application

An application of the platform that you are targeting that consists purely of user interface rendering code, and platform specific application logic. Create your views here and attach your views to your view model using data binding.

Register your platform-specific concrete implementation of your services e.g. `iPhoneNetworkConnectivityService : INetworkConnectivityService`. You can use your favorite IOC library for this.

*Please note that the .Android namespace is reserved by Google for Android internals and must not be used. This is a Google limitation. Failure to obey this will result in heaps of pain.*

# Target Platform Tests

How you handle this is purely up to you. Due to the way Xamarin works, this is a good opportunity to write higher level acceptance tests using Xamarin Test Cloud instead of lower level library tests. The reason behind this is that code behaves differently on physical hardware vs emulated hardware, and that the linker can sometimes be too greedy resulting in code being linked out. Sometimes the only way to pick up on that is to actually run on physical hardware.

# TL;DR

A complete layout could look like the following (these are .csproj files):
- MyCoolApp.Core - Usually a portable class library, contains the core functionality
- MyCoolApp.Core.Tests - Tests for the core functionality
- MyCoolApp.Droid - Device/platform specific view code
- MyCoolApp.iOS - Device/platform specific view code

# UWP/WPF

Moved the xxxUserControl classes to a Views subfolder, and renamed them. This might just be me, but I prefer to call anything that derives from IViewFor to be a "View" and anything that acts as a control (like a custom text box or something) to go into a "Controls" subfolder. Tl;DR: "Views" => RxUI objects, "Controls" => custom Windows controls

## Styles/Resource Dictionaries Convention

Geoff:

>  Whilst I've got your attention matt any recommending reading how to tame resource dictionaries. Inherited a interesting project and all the styles are mixed everywhere, app, in page, in global styles.xaml.

> Does creating one file per style sorted by view name under Styles then merged together by a MyCoolViewStyles. Make sense or does it create perf issues having that many resource dictionaries.

Matt:

> no perf issues I keep mine separate

Kent:
 
> yeah, fwiw @ghuntley, I also separate dictionaries. I have a `Theme.xaml` which merges in individual dictionaries (`Button.xaml`, `TextBox.xaml` etc). Then my `App.xaml` just merges in `Theme.xaml` of course, doing this stuff in Xamarin Forms is far more touch and go


Matt:

> that's what I used to do, but that causes a lot of stuff to get parsed on first load when it's not necessary

> so only the stuff that I ​_know_​ will get used on 95% of all xaml pages get added to the `App`'s resources. things like the color palette

> it means you have to do a little more work inside each user control, because you get stuff like this:

    ```<UserControl.Resources>
        <ResourceDictionary>
            <ResourceDictionary.MergedDictionaries>
                <ResourceDictionary Source="/Resources/Common/ResourceDictionaries/DefaultStyles/PasswordBox.xaml" />
                <ResourceDictionary Source="/Resources/Common/ResourceDictionaries/DefaultStyles/TextBox.xaml" />
                <ResourceDictionary Source="/Resources/Common/ResourceDictionaries/DefaultStyles/ToggleSwitch.xaml" />
                <ResourceDictionary Source="/Resources/Common/ResourceDictionaries/NamedStyles/SmallHeaderStyles.xaml" />
            </ResourceDictionary.MergedDictionaries>
        </ResourceDictionary>
    </UserControl.Resources>
    ```

> but you only take the hit for loading `PasswordBox.xaml` once, when the first page requires it. If you never browse to one of those pages, then your app never takes the time to parse that file

Kent:

> interesting approach. I never ran into any perf walls, so never needed to do anything differently

Matt: 

> it speeds up the initial application load somewhat if you have a lot of styles
    basically takes that initial 50ms of initial app load, and parcels it out in 5-10ms hits on individual pages

> my `Resources` tree usually looks something like this:

    ​[2:40] 
    ```\Resources
    \Common
        \Images
        \ResourceDictionaries
        \Brushes
            ColorPalette.xaml
        \DefaultStyles
            ButtonStyles.xaml
            TextBlockStyles.xaml
            TextBoxStyles.xaml
        \NamedStyles
            LeftHeaderTextBoxStyle.xaml
            TileButtonStyles.xaml
    \LightTheme
        ...repeats...
    \DarkTheme
        ...repeats...
    \HighContrast (or whatever the key is, this might be incorrect)
    ```

Kent:
 
 > I think I’d prefer to make the user wait an extra 50ms (on top of the several seconds they already have to wait for WPF/.NET to bootstrap) than force devs to deal with this. I guess UWP may be different because of native compilation and usage patterns. Desktop software is Different.

Matt:

> it's a tradeoff, for sure on mobile, it's been a noticeable difference when I apply this pattern

> if I were doing WPF, I'd probably just have everything referenced in a single RD which is included in the app.xaml, as you said
> Oh, I found the blog post: http://projekt202.com/blog/2010/xaml-organization/
> I think I've improved upon this, but this blog entry is where I got started with `ResourceDictionary` organization

