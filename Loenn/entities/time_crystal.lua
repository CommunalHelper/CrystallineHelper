local timeCrystal = {}

timeCrystal.name = "vitellary/timecrystal"
timeCrystal.depth = -100

timeCrystal.fieldInformation = {
    stopLength = {
        minimumValue = 0.0
    },
    respawnTime = {
        minimumValue = 0.0
    },
    timeScale = {
        minimumValue = 0.0
    }
}

timeCrystal.placements = {
    {
        name = "normal",
        data = {
            oneUse = false,
            stopLength = 2.0,
            respawnTime = 2.5,
            untilDash = false,
            immediate = false,
            entityTypesToIgnore = "",
            timeScale = 0.0,
			flag = ""
        }
    },
    {
        name = "until_dash",
        data = {
            oneUse = false,
            stopLength = 2.0,
            respawnTime = 2.5,
            untilDash = true,
            immediate = false,
            entityTypesToIgnore = "",
            timeScale = 0.0,
			flag = ""
        }
    }
}

function timeCrystal.texture(room, entity)
    return entity.untilDash and "objects/crystals/time/untildash/idle00" or "objects/crystals/time/idle00"
end

return timeCrystal
