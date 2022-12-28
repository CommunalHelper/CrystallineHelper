module FlushelinePairedDashSwitch

using ..Ahorn, Maple

@mapdef Entity "vitellary/paireddashswitch" PairedDashSwitch(x::Integer, y::Integer, direction::String="Left", groupId::String="", flag::String="", sprite::String="dashSwitch_default", pressed::Bool=false, affectedByFlag::Bool=false)

const placements = Ahorn.PlacementDict(
	"Paired Dash Switch (Left) (Crystalline)" => Ahorn.EntityPlacement(
        PairedDashSwitch,
        "point",
        Dict{String, Any}(
            "direction" => "Left"
        )
    ),
	"Paired Dash Switch (Right) (Crystalline)" => Ahorn.EntityPlacement(
        PairedDashSwitch,
        "point",
        Dict{String, Any}(
            "direction" => "Right"
        )
    ),
	"Paired Dash Switch (Up) (Crystalline)" => Ahorn.EntityPlacement(
        PairedDashSwitch,
        "point",
        Dict{String, Any}(
            "direction" => "Up"
        )
    ),
	"Paired Dash Switch (Down) (Crystalline)" => Ahorn.EntityPlacement(
        PairedDashSwitch,
        "point",
        Dict{String, Any}(
            "direction" => "Down"
        )
    )
)

const directions = ["Left", "Right", "Up", "Down"]

Ahorn.nodeLimits(entity::PairedDashSwitch) = 0, 1

Ahorn.editingOptions(entity::PairedDashSwitch) = Dict{String, Any}(
    "direction" => directions
)
Ahorn.editingIgnored(entity::PairedDashSwitch, multiple::Bool=false) = multiple ? String["x", "y", "nodes", "flag", "pressed", "direction"] : String[]

function Ahorn.selection(entity::PairedDashSwitch)
    x, y = Ahorn.position(entity)
    dir = get(entity.data, "direction", "Left")

    res = Ahorn.Rectangle[]

    if dir == "Left"
		push!(res, Ahorn.Rectangle(x - 2, y, 10, 16))
    elseif dir == "Right"
        push!(res, Ahorn.Rectangle(x, y, 10, 16))
	elseif dir == "Up"
		push!(res, Ahorn.Rectangle(x, y - 4, 16, 12))
	elseif dir == "Down"
		push!(res, Ahorn.Rectangle(x, y, 16, 12))
    end

    nodes = get(entity.data, "nodes", ())
    for node in nodes
        nx, ny = Int.(node)
        
        push!(res, Ahorn.Rectangle(nx, ny, 8, 8))
    end

    return res
end

function Ahorn.renderSelectedAbs(ctx::Ahorn.Cairo.CairoContext, entity::PairedDashSwitch)
    x, y = Ahorn.position(entity)

    sx = 0
    sy = 0

    dir = get(entity.data, "direction", "Left")
    if (dir == "Left" || dir == "Right")
        sx = x + 4
        sy = y + 8
    elseif (dir == "Up" || dir == "Down")
        sx = x + 8
        sy = y + 4
    end

    nodes = get(entity.data, "nodes", ())
    for node in nodes
        nx, ny = Int.(node)

        theta = atan(sy - ny, sx - nx)
        Ahorn.drawArrow(ctx, sx, sy, nx + cos(theta) + 4, ny + sin(theta) + 4, Ahorn.colors.selection_selected_fc, headLength=6)
        Ahorn.drawRectangle(ctx, nx, ny, 8, 8, Ahorn.colors.trigger_fc, Ahorn.colors.trigger_bc)
    end
end

function Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::PairedDashSwitch, room::Maple.Room)
    dir = get(entity.data, "direction", "Left")
    pressed = get(entity.data, "pressed", false)

    sprite = pressed ? "objects/temple/dashButton26" : "objects/temple/dashButton00"
	
	if dir == "Left"
        Ahorn.drawSprite(ctx, sprite, pressed ? 13 : 7, 8)
    elseif dir == "Right"
		Ahorn.drawSprite(ctx, sprite, pressed ? 14 : 20, 26, rot=pi)
	elseif dir == "Up"
		Ahorn.drawSprite(ctx, sprite, 27, pressed ? 13 : 7, rot=pi / 2)
	elseif dir == "Down"
		Ahorn.drawSprite(ctx, sprite, 9, pressed ? 14 : 20, rot=-pi / 2)
    end
end

end