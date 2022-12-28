local utils = require "utils"
local drawableSprite = require "structs.drawable_sprite"

local forcefield = {}

forcefield.name = "vitellary/forcefield"
forcefield.nodeLimits = {1, -1}
forcefield.nodeVisibility = "never"

forcefield.placements = {
    {
        name = "forcefield8x8",
        data = {
            texture = "forcefield/1tile",
            tint = "5feeff",
            flag = "",
            visibleDistance = 0,
            allowClipping = true,
        }
    },
    {
        name = "forcefield16x16",
        data = {
            texture = "forcefield/2tile",
            tint = "5feeff",
            flag = "",
            visibleDistance = 0,
            allowClipping = true,
        }
    },
}

function forcefield.sprite(room, entity)
    local x, y = entity.x or 0, entity.y or 0
    local nodes = entity.nodes or {{x = 0, y = 0}, {x = 0, y = 0}}
    local color = utils.getColor(entity.tint or "ffffff")
    local path = (entity.texture or "forcefield/1tile")

    local sprites = {}
    for i=0,#nodes do
        local node = {x = x, y = y}
        if nodes[i] then
            node = {x = nodes[i].x, y = nodes[i].y}
        end
        local nodesprite = drawableSprite.fromTexture("objects/"..path.."/end00", node)
        nodesprite:setColor(color)
        table.insert(sprites, nodesprite)

        if i ~= #nodes then
            local next_node = {x = nodes[i+1].x, y = nodes[i+1].y}

            local angle = math.atan2(next_node.y - node.y, next_node.x - node.x)
            local start_x, start_y = node.x + 4*math.cos(angle), node.y + 4*math.sin(angle)
            local end_x, end_y = next_node.x - 4*math.cos(angle), next_node.y - 4*math.sin(angle)
            local dist = math.sqrt(((end_x - start_x)^2) + ((end_y - start_y)^2))

            local lasersprite = drawableSprite.fromTexture("objects/"..path.."/laser00", {
                x = start_x,
                y = start_y,
                jx = 0,
                color = color,
            })
            lasersprite.scaleX = dist / lasersprite.meta.width
            lasersprite.rotation = angle
            table.insert(sprites, lasersprite)
        end
    end

    return sprites
end

function forcefield.selection(room, entity)
    local x, y = entity.x or 0, entity.y or 0
    local nodes = entity.nodes or {{x = 0, y = 0}, {x = 0, y = 0}}
    local path = (entity.texture or "forcefield/1tile")
    local nodesprite = drawableSprite.fromTexture("objects/"..path.."/end00", node)
    local w, h = nodesprite.meta.width, nodesprite.meta.height

    local rects = {}
    for i, node in ipairs(nodes) do
        table.insert(rects, utils.rectangle(node.x - w/2, node.y - h/2, w, h))
    end

    return utils.rectangle(x - w/2, y - h/2, w, h), rects
end

return forcefield