local utils = require "utils"
local drawableSprite = require "structs.drawable_sprite"

local energyBooster = {}

energyBooster.name = "vitellary/energybooster"
energyBooster.placements = {
    {
        name = "normal",
        data = {
            behaveLikeDash = false,
            redirectSpeed = false,
            oneUse = false,
        },
    },
    {
        name = "redirect",
        data = {
            behaveLikeDash = false,
            redirectSpeed = true,
            oneUse = false,
        },
    },
}

function energyBooster.sprite(room, entity)
    return drawableSprite.fromTexture(entity.redirectSpeed and "objects/energyBoosterRedirect/booster00" or "objects/energyBooster/booster00", entity)
end

function energyBooster.selection(room, entity)
    return utils.rectangle(entity.x - 10, entity.y - 10, 20, 20)
end

return energyBooster