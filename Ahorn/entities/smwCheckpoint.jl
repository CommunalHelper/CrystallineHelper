module FlushelineSMWCheckpoint

using ..Ahorn, Maple

@mapdef Entity "vitellary/smwcheckpoint" Checkpoint(x::Integer, y::Integer, height::Integer=16, fullHeight::Bool=false)

const placements = Ahorn.PlacementDict(
    "SMW Checkpoint (Crystalline)" => Ahorn.EntityPlacement(
        Checkpoint,
        "rectangle",
        Dict{String, Any}(),
        function(entity)
            entity.data["nodes"] = [(Int(entity.data["x"]), Int(entity.data["y"]) + 8)]
        end
    )
)

Ahorn.nodeLimits(entity::Checkpoint) = 1, 1

function Ahorn.selection(entity::Checkpoint)
    x, y = Ahorn.position(entity)
    height = Int(get(entity.data, "height", 16))
    node = get(entity.data, "nodes", ())[1]
    
    return [Ahorn.Rectangle(x, y, 16, height), Ahorn.Rectangle(x+2, node[2], 12, 4)]
end

Ahorn.minimumSize(entity::Checkpoint) = 0, 16
Ahorn.resizable(entity::Checkpoint) = false, true

function Ahorn.renderAbs(ctx::Ahorn.Cairo.CairoContext, entity::Checkpoint, room::Maple.Room)
    x, y = Ahorn.position(entity)
    height = Int(get(entity.data, "height", 16))
    node = get(entity.data, "nodes", ())[1]
    barHeight = node[2]
    
    for i in 0:(floor(Int, height / 8) - 1)
        if i == 0
            Ahorn.drawImage(ctx, "objects/smwCheckpoint/bars", x, y, 0, 0, 4, 8)
        else
            Ahorn.drawImage(ctx, "objects/smwCheckpoint/bars", x, y + i*8, 0, 8, 4, 8)
        end
    end
    Ahorn.drawImage(ctx, "objects/smwCheckpoint/cp", x+2, barHeight)
    for i in 0:(floor(Int, height / 8) - 1)
        if i == 0
            Ahorn.drawImage(ctx, "objects/smwCheckpoint/bars", x + 12, y, 4, 0, 4, 8)
        else
            Ahorn.drawImage(ctx, "objects/smwCheckpoint/bars", x + 12, y + i*8, 4, 8, 4, 8)
        end
    end
end

end