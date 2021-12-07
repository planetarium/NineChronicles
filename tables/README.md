# Installation 

## 1.Install Julia
Install Julia 1.7.0 and VSCode. See [instruction here](https://code.visualstudio.com/docs/languages/julia)

## 2.Install GameDataManager package 
Start julia REPL, then enter the package mode by typing `]` on your keyboard 
then run commands below
``` julia 
pkg>add https://github.com/YongHee-Kim/GameDataManager.jl
```
See [instruction here](https://docs.julialang.org/en/v1/stdlib/Pkg/) for Julia package management

# Loading the NineChronicle project
All the necessary configulation are already written on the [config.json](config.json). 
you need to give GameDataManager directory of [config.json](config.json) to load your project. 

```julia
julia>cd(".../NineChronicles/tables")
julia>using GameDataManager
[ Info: "NineChronicles" Project has loaded successfully!
```
If `[ Info: "NineChronicles" Project has loaded successfully!` is not showing up, then your julia session path is wrong. Check path information with 
```julia
julia>pwd()
"C:\\Users\\Maste\\Projects\\Planetarium\\NineChronicles"
```
You can manually load your project by feeding path to your [config.json](config.json) with 
```julia
julia>using GameDataManager
julia>init_project(joinpath(pwd(), "tables"))
[ Info: "NineChronicles" Project has loaded successfully!
```

# Custom Functions
 GameDataManager has full access to all your gamedata. You can do wonders with such power. GameDataManger is aiming to let any game designers to write down necessary julia script and distribute to the team. Those scripts would be prone to bug and might not be maintainable in the long run. But that's **OK** because those scripts will resides in the seperate package, and won't jeopardize others.

For demonstration I've put something inside [NineChronicles](.\julia\NineChronicles\src\NineChronicles.jl) to help with managing localization data with Excel. 

You can use it after [loading the project](#Loading-the-NineChronicle-project)
```julia
julia>using NineChronicles
julia>build_localize()
┌ 연산결과: Localization 데이터를 XLSX로 병합하였습니다
└    SAVED => C:\Users\Maste\Projects\Planetarium\NineChronicles\tables\localization\Localization.xlsx
```

# More On GameDataManager
[GameDataManger](https://github.com/YongHee-Kim/GameDataManager.jl/blob/main/README_KR.md) is still in development, and not ready for full deployment. Contact @YongHee-Kim for any help or bugs.