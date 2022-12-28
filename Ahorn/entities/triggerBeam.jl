module FlushelineTriggerBeam

using ..Ahorn, Maple

@mapdef Entity "vitellary/triggerbeam" TriggerBeam(x::Integer, y::Integer, width::Integer=8, height::Integer=8, direction::String="Up", color::String="FFFFFF", flag::String="", exitNodes::String="", alpha::Number=0.5, inactiveAlpha::Number=0.0, invertFlag::Bool=false, exitAlwaysActive::Bool=false, attachToSolids::Bool=false)

const placements = Ahorn.PlacementDict(
    "Trigger Beam (Up) (Crystalline)" => Ahorn.EntityPlacement(
        TriggerBeam,
        "rectangle",
        Dict{String, Any}(
            "direction" => "Up"
        )
    ),
    "Trigger Beam (Down) (Crystalline)" => Ahorn.EntityPlacement(
        TriggerBeam,
        "rectangle",
        Dict{String, Any}(
            "direction" => "Down"
        )
    ),
    "Trigger Beam (Left) (Crystalline)" => Ahorn.EntityPlacement(
        TriggerBeam,
        "rectangle",
        Dict{String, Any}(
            "direction" => "Left"
        )
    ),
    "Trigger Beam (Right) (Crystalline)" => Ahorn.EntityPlacement(
        TriggerBeam,
        "rectangle",
        Dict{String, Any}(
            "direction" => "Right"
        )
    )
)

Ahorn.nodeLimits(entity::TriggerBeam) = 0, -1
Ahorn.minimumSize(entity::TriggerBeam) = 8, 8
Ahorn.editingOrder(entity::TriggerBeam) = String["x", "y", "width", "height", "inactiveAlpha", "color", "alpha", "flag", "exitNodes", "invertFlag", "exitAlwaysActive", "attachToSolids", "nodes"]

const directionVectors = Dict{String, Tuple{Int, Int}}(
    "Up" => (0, -1),
    "Down" => (0, 1),
    "Left" => (-1, 0),
    "Right" => (1, 0)
)

function Ahorn.editingIgnored(entity::TriggerBeam, multiple::Bool=false)
    if multiple
        return String["x", "y", "width", "height", "direction", "nodes"]
    else
        dir = get(entity.data, "direction", "Up")
        if dir == "Left" || dir == "Right"
            return String["width", "direction"]
        else
            return String["height", "direction"]
        end
    end
end

function Ahorn.resizable(entity::TriggerBeam)
    dir = get(entity.data, "direction", "Up")

    if dir == "Left" || dir == "Right"
        return (false, true)
    else
        return (true, false)
    end
end

function Ahorn.flipped(entity::TriggerBeam, horizontal::Bool)
    dir = get(entity.data, "direction", "Up")

    if horizontal
        if dir == "Left"
            entity.data["direction"] = "Right"
        elseif dir == "Right"
            entity.data["direction"] = "Left"
        end
    else
        if dir == "Up"
            entity.data["direction"] = "Down"
        elseif dir == "Down"
            entity.data["direction"] = "Up"
        end
    end

    return entity
end

function Ahorn.selection(entity::TriggerBeam)
    x, y = Ahorn.position(entity)
    width = get(entity.data, "width", 0)
    height = get(entity.data, "height", 0)

    dx, dy = directionVectors[get(entity.data, "direction", "Up")]

    ox = (dx != 0) ? -8 : 0
    oy = (dy != 0) ? -8 : 0
    ow = (dx != 0) ? 8 : 0
    oh = (dy != 0) ? 8 : 0

    res = Ahorn.Rectangle[Ahorn.Rectangle(x + ox, y + oy, width + ow, height + oh)]

    nodes = get(entity.data, "nodes", ())
    for node in nodes
        nx, ny = Int.(node)
        
        push!(res, Ahorn.Rectangle(nx, ny, 8, 8))
    end

    return res
end

function Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::TriggerBeam, room::Maple.Room)
    x, y = Ahorn.position(entity)
    width = Int(get(entity.data, "width", 0))
    height = Int(get(entity.data, "height", 0))

    dx, dy = directionVectors[get(entity.data, "direction", "Up")]
    size = (dy != 0) ? width : height

    ox = (dx < 0) ? -8 : 0
    oy = (dy < 0) ? -8 : 0

    rawColor = Ahorn.argb32ToRGBATuple(parse(Int, get(entity.data, "color", "ffffff"), base=16))[1:3] ./ 255
    color = (rawColor..., 0.4)

    for i in 0:(floor(Int, size / 8) - 1)
        bx = x + (abs(dy) * i * 8)
        by = y + (abs(dx) * i * 8)

        length = max(8, getLength(bx + ox, by + oy, dx, dy, room))

        rx = bx + (length * dx) + (abs(dy) * 8)
        ry = by + (length * dy) + (abs(dx) * 8)

        x1 = min(bx, rx)
        y1 = min(by, ry)
        x2 = max(bx, rx)
        y2 = max(by, ry)

        Ahorn.drawRectangle(ctx, x1 - x, y1 - y, x2 - x1, y2 - y1, color)

        rot = atan(dy, dx)
        for j in 0:(floor(Int, length / 8) - 1)
            drawx = bx - x + (dx * 8 * j) + 4 + (dy > 0 ? 8 : 0)
            drawy = by - y + (dy * 8 * j) + 4 + (dx < 0 ? 8 : 0)
            Ahorn.drawSprite(ctx, "ahorn_triggerbeamdir", drawx, drawy, rot = rot, tint = color, alpha = 0.25)
        end
    end
end

function Ahorn.renderSelectedAbs(ctx::Ahorn.Cairo.CairoContext, entity::TriggerBeam)
    x, y = Ahorn.position(entity)
    width = get(entity.data, "width", 0)
    height = get(entity.data, "height", 0)

    dx, dy = directionVectors[get(entity.data, "direction", "Up")]

    sx = x + (width / 2) + ((dx < 0) ? -8 : 0)
    sy = y + (height / 2) + ((dy < 0) ? -8 : 0)

    exitNodes = split(get(entity.data, "exitNodes", ""), ",", keepempty=false)

    nodes = get(entity.data, "nodes", ())
    for i in 1:length(nodes)
        node = nodes[i]
        nx, ny = Int.(node)

        isexit = string(i) in exitNodes

        theta = atan(sy - ny, sx - nx)
        Ahorn.drawArrow(ctx, sx, sy, nx + cos(theta) + 4, ny + sin(theta) + 4, isexit ? Ahorn.colors.selection_selected_fc : Ahorn.colors.selection_selection_fc, headLength=6)
        Ahorn.drawRectangle(ctx, nx, ny, 8, 8, (0.0, 0.0, 1.0, 0.6), (0.0, 0.0, 1.0, 0.4))
    end

    ox = (dx != 0) ? -8 : 0
    oy = (dy != 0) ? -8 : 0
    ow = (dx != 0) ? 8 : 0
    oh = (dy != 0) ? 8 : 0

    Ahorn.drawRectangle(ctx, x + ox, y + oy, width + ow, height + oh, Ahorn.colors.trigger_fc, Ahorn.colors.trigger_bc)
end

function getLength(x::Integer, y::Integer, dx::Integer, dy::Integer, room::Maple.Room)
    width, height = room.size
    tx, ty = floor(Int, x / 8) + 1, floor(Int, y / 8) + 1

    maxLength = 0
    if dx > 0
        maxLength = width - x
    elseif dx < 0
        maxLength = x
    elseif dy > 0
        maxLength = height - y
    elseif dy < 0
        maxLength = y
    end

    wantedLength = 0
    while wantedLength <= maxLength
        if get(room.fgTiles.data, (ty, tx), '0') != '0'
            break
        end

        wantedLength += 8
        tx += dx
        ty += dy
    end

    return wantedLength
end

end