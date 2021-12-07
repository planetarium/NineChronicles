module NineChronicles

using XLSX, JSON
using OrderedCollections
using GameDataManager 
import GameDataManager: GAMEENV, CACHE

export build_localize

"""
    build_localize

$(GAMEENV["LOCALIZE"]) 경로에 있는 데이터를 모두 모아\n
파일명을 각각의 시트로 하는 XLSX 파일을 생성해 줍니다. 
"""
function build_localize()
    fname = joinpath(GAMEENV["LOCALIZE"], "Localization.xlsx")
    localizedatas = load_localizedata()

    XLSX.openxlsx(fname, mode="w") do xf
        for (i, (fname, data)) in enumerate(localizedatas)
            a = split(fname, "_")
            name = string(a[1])
            language = string(a[2])
            if i == 1 
                XLSX.rename!(xf[1], name)
            else
                XLSX.addsheet!(xf, name)
            end  
            _keys = collect(keys(data))
            _vals = string.(collect(values(data)))
            XLSX.writetable!(xf[i], [_keys], ["Key"]; anchor_cell = XLSX.CellRef("A1"))
            XLSX.writetable!(xf[i], [_vals], [language]; anchor_cell = XLSX.CellRef("B1"))
        end
    end
    GameDataManager.print_write_result(fname, "Localization 데이터를 XLSX로 병합하였습니다")
end


function load_localizedata()
    localizedatas = OrderedDict{String, AbstractDict}()
    for file in readdir(GAMEENV["LOCALIZE"]; join = true )
        if endswith(file, ".json")
            k = splitext(basename(file))[1]
            localizedatas[k] = open(file, "r") do io 
                JSON.parse(io; dicttype=OrderedDict)
            end
        end
    end
    return localizedatas
end

end # module
