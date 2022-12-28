local utils = require "utils"
local enums = require "consts.celeste_enums"
local drawableSprite = require "structs.drawable_sprite"

local deadlySwitch = {}

deadlySwitch.name = "vitellary/deadlydashswitch"
deadlySwitch.placements = {}

for _,dir in ipairs(enums.dash_switch_sides) do
    table.insert(deadlySwitch.placements, {
        name = "deadlyswitch_"..string.lower(dir),
        data = {
            direction = dir,
            persistent = false,
        }
    })
end
deadlySwitch.fieldInformation = {
    direction = {
        editable = false,
        options = enums.dash_switch_sides,
    },
}

deadlySwitch.justification = {0.5, 0.5}
function deadlySwitch.sprite(room, entity)
    local sprite = drawableSprite.fromTexture("objects/deadlyDashButton/dashButton00", entity)
    if entity.direction == "Right" then
        sprite.rotation = math.pi
        sprite:addPosition(0, 8)
    elseif entity.direction == "Down" then
        sprite.rotation = -math.pi/2
        sprite:addPosition(8, 0)
    elseif entity.direction == "Up" then
        sprite.rotation = math.pi/2
        sprite:addPosition(8, 8)
    else
        sprite.rotation = 0
        sprite:addPosition(8, 8)
    end
    return sprite
end

function deadlySwitch.selection(room, entity)
    local x, y = entity.x or 0, entity.y or 0
    if entity.direction == "Right" then
        return utils.rectangle(x,y,8,16)
    elseif entity.direction == "Down" then
        return utils.rectangle(x,y,16,8)
    elseif entity.direction == "Up" then
        return utils.rectangle(x,y,16,8)
    else
        return utils.rectangle(x,y,8,16)
    end
end

return deadlySwitch