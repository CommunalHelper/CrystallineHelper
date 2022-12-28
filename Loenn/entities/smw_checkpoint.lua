local utils = require "utils"
local drawableSprite = require "structs.drawable_sprite"

local smwCheckpoint = {}

smwCheckpoint.name = "vitellary/smwcheckpoint"
smwCheckpoint.placements = {
    {
        name = "checkpoint",
        data = {
            height = 16,
            fullHeight = false,
        },
    },
}
smwCheckpoint.nodeLimits = {1, 1}
smwCheckpoint.nodeVisibility = "never"
smwCheckpoint.canResize = {false, true}
smwCheckpoint.minimumSize = {0, 16}

function smwCheckpoint.sprite(room, entity)
    local sprites = {}
    
    for i=0, math.floor(entity.height / 8) - 1 do
        local quad_x, quad_y = 0, (i == 0) and 0 or 8
        local sprite = drawableSprite.fromTexture("objects/smwCheckpoint/bars", entity)
        sprite.y += i*8
        sprite:useRelativeQuad(quad_x, quad_y, 4, 8)
        table.insert(sprites, sprite)
    end

    local cp = drawableSprite.fromTexture("objects/smwCheckpoint/cp", entity)
    cp:setPosition(entity.x + 2, entity.nodes[1].y)
    cp:setJustification(0, 0.5)
    table.insert(sprites, cp)

    for i=0, math.floor(entity.height / 8) - 1 do
        local quad_x, quad_y = 4, (i == 0) and 0 or 8
        local sprite = drawableSprite.fromTexture("objects/smwCheckpoint/bars", entity)
        sprite:addPosition(12, i*8)
        sprite:useRelativeQuad(quad_x, quad_y, 4, 8)
        table.insert(sprites, sprite)
    end

    return sprites
end

function smwCheckpoint.selection(room, entity)
    local x, y, w, h = entity.x or 0, entity.y or 0, 16, entity.height or 16
    return utils.rectangle(x, y, w, h), {utils.rectangle(x+2, (entity.nodes[1].y or y) - 2, 12, 4)}
end

return smwCheckpoint