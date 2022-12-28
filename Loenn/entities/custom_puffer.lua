local customPuffer = {}

customPuffer.name = "vitellary/custompuffer"
customPuffer.placements = {
    {
        name = "puffer_right",
        data = {
            right = true,
            static = false,
            alwaysShowOutline = true,
            pushAnyDir = false,
            oneUse = false,
            angle = 0,
            radius = 32,
            launchSpeed = 280,
            respawnTime = 2.5,
            sprite = "pufferFish",
            deathFlag = "",
            holdable = false,
            outlineColor = "FFFFFF",
            returnToStart = true,
            holdFlip = false,
            boostMode = "SetSpeed",
        }
    },
    {
        name = "puffer_left",
        data = {
            right = false,
            static = false,
            alwaysShowOutline = true,
            pushAnyDir = false,
            oneUse = false,
            angle = 0,
            radius = 32,
            launchSpeed = 280,
            respawnTime = 2.5,
            sprite = "pufferFish",
            deathFlag = "",
            holdable = false,
            outlineColor = "FFFFFF",
            returnToStart = true,
            holdFlip = false,
            boostMode = "SetSpeed",
        }
    }
}
customPuffer.fieldInformation = {
    radius = {
        fieldType = "integer",
    },
    outlineColor = {
        fieldType = "color",
    },
    boostMode = {
        editable = false,
        options = {
            ["Set Speed"] = "SetSpeed",
            ["Redirect Speed"] = "RedirectSpeed",
            ["Redirect + Add Speed"] = "AddRedirectSpeed",
        }
    }
}

customPuffer.depth = 0
customPuffer.texture = "objects/puffer/idle00"

function customPuffer.scale(room, entity)
    local right = entity.right

    return right and 1 or -1, 1
end

return customPuffer