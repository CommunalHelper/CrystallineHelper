local utils = require "utils"
local drawableRect = require "structs.drawable_rectangle"
local drawableSprite = require "structs.drawable_sprite"

local triggerBeam = {}

triggerBeam.name = "vitellary/triggerbeam"
local dirs = {"Right", "Down", "Left", "Up"}
triggerBeam.placements = {}
for _,dir in ipairs(dirs) do
    table.insert(triggerBeam.placements, {
        name = "beam_"..string.lower(dir),
        data = {
            width = 8,
            height = 8,
            direction = dir,
            color = "FFFFFF",
            flag = "",
            exitNodes = "",
            alpha = 0.5,
            inactiveAlpha = 0.0,
            invertFlag = false,
            exitAlwaysActive = false,
            attachToSolids = false,
        }
    })
end
triggerBeam.fieldInformation = {
    direction = {
        editable = false,
        options = dirs,
    },
}
triggerBeam.minimumSize = {8, 8}
triggerBeam.canResize = {true, true}
triggerBeam.nodeLimits = {0, -1}
triggerBeam.nodeLineRenderType = "fan"

local vectors = {
    Right = {1, 0},
    Down = {0, 1},
    Left = {-1, 0},
    Up = {0, -1},
}

local function getLength(x, y, dx, dy, room)
    local w, h = room.width, room.height
    local tx, ty = math.floor(x/8) + 1, math.floor(y/8) + 1

    local maxLength = 0
    if dx > 0 then
        maxLength = w - x - 8
    elseif dx < 0 then
        maxLength = x
    elseif dy > 0 then
        maxLength = h - y - 8
    elseif dy < 0 then
        maxLength = y
    end

    local wantedLength = 0
    while wantedLength <= maxLength do
        local t_matrix = ((ty-1)*math.floor(room.width/8)) + tx
        local tile = room.tilesFg.matrix[t_matrix]
        if tile and tile ~= "0" then
            break
        end

        wantedLength += 8
        tx += dx
        ty += dy
    end

    return wantedLength
end

function triggerBeam.sprite(room, entity)
    local x, y, w, h = entity.x or 0, entity.y or 0, entity.width or 8, entity.height or 8
    local dx, dy = unpack(vectors[entity.direction])

    local size = (dy ~= 0) and w or h
    local ox, oy = (dx < 0) and -8 or 0, (dy < 0) and -8 or 0

    local color = utils.getColor(entity.color)
    local beamColor = {color[1], color[2], color[3], 0.4}

    local sprites = {}

    for i=0, math.floor(size/8)-1 do
        local bx, by = x + (math.abs(dy) * i * 8), y + (math.abs(dx) * i * 8)
        local length = math.max(getLength(bx + ox, by + oy, dx, dy, room), 8)

        local rx = bx + (length * dx) + (math.abs(dy) * 8)
        local ry = by + (length * dy) + (math.abs(dx) * 8)

        local x1 = math.min(bx, rx)
        local y1 = math.min(by, ry)
        local x2 = math.max(bx, rx)
        local y2 = math.max(by, ry)

        local rect = drawableRect.fromRectangle("fill", x1, y1, x2 - x1, y2 - y1, beamColor)
        table.insert(sprites, rect)

        local rot = math.atan2(dy, dx)
        for j=0, math.floor(length/8)-1 do
            local drawx = bx + (dx * 8 * j) + 4 + ((dx < 0) and -8 or 0)
            local drawy = by + (dy * 8 * j) + 4 + ((dy < 0) and -8 or 0)
            local arrow = drawableSprite.fromTexture("ahorn_triggerbeamdir", {x = drawx, y = drawy, rotation = rot})
            table.insert(sprites, arrow)
        end
    end

    return sprites
end

function triggerBeam.selection(room, entity)
    local x, y, w, h = entity.x or 0, entity.y or 0, entity.width or 8, entity.height or 8

    local dx, dy = unpack(vectors[entity.direction])

    local size = (dy ~= 0) and w or h
    local ox, oy = (dx < 0) and -8 or 0, (dy < 0) and -8 or 0

    local maxLength = 8
    for i=0, math.floor(size/8)-1 do
        local bx, by = x + (math.abs(dy) * i * 8), y + (math.abs(dx) * i * 8)
        maxLength = math.max(getLength(bx + ox, by + oy, dx, dy, room), maxLength)
    end

    if dx == 0 then
        if dy < 0 then
            y -= maxLength
        end
        return utils.rectangle(x, y, size, maxLength)
    else
        if dx < 0 then
            x -= maxLength
        end
        return utils.rectangle(x, y, maxLength, size)
    end
end

return triggerBeam