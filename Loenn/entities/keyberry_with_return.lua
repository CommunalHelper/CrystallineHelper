local utils = require "utils"
local drawableSprite = require "structs.drawable_sprite"
local drawableLine = require "structs.drawable_line"

local returnKeyberry = {}

returnKeyberry.name = "vitellary/returnkeyberry"
returnKeyberry.depth = -100

returnKeyberry.nodeLimits = {2, -1}

returnKeyberry.placements = {
    {
        name = "normal",
        data = {
            winged = false
        }
    },
    {
        name = "winged",
        data = {
            winged = true
        }
    }
}

function returnKeyberry.sprite(room, entity)
    local winged = entity.winged

    local texture = winged and "collectables/keyberry/wings00" or "collectables/keyberry/normal03"
    local sprite = drawableSprite.fromTexture(texture, entity)
    if winged then
        sprite:addPosition(0, 1)
    end

    return sprite
end

function returnKeyberry.nodeSprite(room, entity, node, nodeIndex, viewport)
    local x, y = entity.x or 0, entity.y or 0
    local nodes = entity.nodes or {{x = 0, y = 0}, {x = 0, y = 0}}

    local bubble = nodeIndex <= 2
    local sprite = drawableSprite.fromTexture(bubble and "characters/player/bubble" or "collectables/keyberry/seed00", node)

    if nodeIndex == 2 then
        x, y = nodes[1].x, nodes[1].y
    end
    local line = drawableLine.fromPoints({x, y, node.x, node.y}, {1, 0, 1, 0.4})

    return {sprite, line}
end

function returnKeyberry.selection(room, entity)
    local x, y = entity.x or 0, entity.y or 0
    local nodes = entity.nodes or {{x = 0, y = 0}, {x = 0, y = 0}}

    local rects = {}
    for i, node in ipairs(nodes) do
        if i <= 2 then
            table.insert(rects, utils.rectangle(node.x - 11, node.y - 11, 23, 23))
        else
            table.insert(rects, utils.rectangle(node.x - 5, node.y - 4, 9, 10))
        end
    end

    return utils.rectangle(x - 8, y - 8, 16, 16), rects
end

return returnKeyberry
