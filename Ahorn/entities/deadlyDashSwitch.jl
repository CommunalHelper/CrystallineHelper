module FlushelineDeadlyDashSwitch

using ..Ahorn, Maple

@mapdef Entity "vitellary/deadlydashswitch" DeadSwitch(x::Integer, y::Integer, direction::String="Left", persistent::Bool=false)

const placements = Ahorn.PlacementDict(
	"Deadly Dash Switch (Left) (Crystalline)" => Ahorn.EntityPlacement(
        DeadSwitch,
        "point",
        Dict{String, Any}(
            "direction" => "Left"
        )
    ),
	"Deadly Dash Switch (Right) (Crystalline)" => Ahorn.EntityPlacement(
        DeadSwitch,
        "point",
        Dict{String, Any}(
            "direction" => "Right"
        )
    ),
	"Deadly Dash Switch (Up) (Crystalline)" => Ahorn.EntityPlacement(
        DeadSwitch,
        "point",
        Dict{String, Any}(
            "direction" => "Up"
        )
    ),
	"Deadly Dash Switch (Down) (Crystalline)" => Ahorn.EntityPlacement(
        DeadSwitch,
        "point",
        Dict{String, Any}(
            "direction" => "Down"
        )
    )
)

const directions = ["Left", "Right", "Up", "Down"]

Ahorn.editingOptions(entity::DeadSwitch) = Dict{String, Any}(
    "direction" => directions
)

function Ahorn.selection(entity::DeadSwitch)
    x, y = Ahorn.position(entity)
    dir = get(entity.data, "direction", "Left")

    if dir == "Left"
		return Ahorn.Rectangle(x - 2, y, 10, 16)
    elseif dir == "Right"
        return Ahorn.Rectangle(x, y, 10, 16)
	elseif dir == "Up"
		return Ahorn.Rectangle(x, y - 4, 16, 12)
	elseif dir == "Down"
		return Ahorn.Rectangle(x, y, 16, 12)
    end
end

function Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::DeadSwitch, room::Maple.Room)
    dir = get(entity.data, "direction", "Left")
	
	if dir == "Left"
        Ahorn.drawSprite(ctx, "objects/deadlyDashButton/dashButton00.png", 7, 8)
    elseif dir == "Right"
		Ahorn.drawSprite(ctx, "objects/deadlyDashButton/dashButton00.png", 49, 56, rot=pi)
	elseif dir == "Up"
		Ahorn.drawSprite(ctx, "objects/deadlyDashButton/dashButton00.png", 56, 7, rot=pi / 2)
	elseif dir == "Down"
		Ahorn.drawSprite(ctx, "objects/deadlyDashButton/dashButton00.png", 8, 49, rot=-pi / 2)
    end
end

end