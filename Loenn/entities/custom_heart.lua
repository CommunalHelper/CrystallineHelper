local drawableSprite = require "structs.drawable_sprite"

local customHeart = {}

customHeart.name = "vitellary/customheart"
customHeart.placements = {
    {
        name = "custom_heart",
        data = {
            slowdown = false,
            endLevel = false,
            oneUse = false,
            respawnTime = 3,
            poemId = "",
            type = "Blue",
            path = "heartGemColorable",
            color = "ff4fed",
            bloom = 0.75,
            light = true,
            bully = false,
            additionalEffects = true,
            switchCoreMode = false,
            colorGrade = false,
            static = false,
            dashCount = 1,
        }
    }
}
customHeart.fieldInformation = {
    type = {
        editable = false,
        options = {
            "Blue",
            "Red",
            "Gold",
            "Custom",
            "Core",
            "CoreInverted",
            "Random",
        },
    },
    dashCount = {
        fieldType = "integer",
    },
}

local function getSprite(entity)
    if entity.type == "Blue" then
        return "collectables/heartGem/0/00"
    elseif entity.type == "Red" then
        return "collectables/heartGem/1/00"
    elseif entity.type == "Gold" then
        return "collectables/heartGem/2/00"
    elseif entity.type == "Custom" then
        return "collectables/"..entity.path.."/00"
    elseif entity.type == "Core" then
        return "ahorn_customcoreheart"
    elseif entity.type == "CoreInverted" then
        return "ahorn_customcoreheartinverted"
    else
        return "collectables/heartGem/0/00"
    end
end

function customHeart.sprite(room, entity)
    local sprite = drawableSprite.fromTexture(getSprite(entity), {x = entity.x, y = entity.y})
    if entity.type == "Custom" then
        sprite:setColor(entity.color)
    end
    return sprite
end

return customHeart