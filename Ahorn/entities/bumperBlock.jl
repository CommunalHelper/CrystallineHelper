module FlushelineBumperBlock

using ..Ahorn, Maple

@mapdef Entity "vitellary/bumperblock" BumperBlock(x::Integer, y::Integer, width::Integer=16, height::Integer=16, axes::String="both")

const placements = Ahorn.PlacementDict(
    "Bumper Block (Both) (Crystalline)" => Ahorn.EntityPlacement(
        BumperBlock,
        "rectangle"
    ),
    "Bumper Block (Vertical) (Crystalline)" => Ahorn.EntityPlacement(
        BumperBlock,
        "rectangle",
        Dict{String, Any}(
            "axes" => "vertical"
        )
    ),
    "Bumper Block (Horizontal) (Crystalline)" => Ahorn.EntityPlacement(
        BumperBlock,
        "rectangle",
        Dict{String, Any}(
            "axes" => "horizontal"
        )
    ),
)

const frameImage = Dict{String, String}(
    "none" => "objects/bumperBlock/block00",
    "horizontal" => "objects/bumperBlock/block01",
    "vertical" => "objects/bumperBlock/block02",
    "both" => "objects/bumperBlock/block03"
)

const face = "objects/bumperBlock/idle_face"
const faceColor = (98, 34, 43) ./ 255
const bumperAxes = String["both", "horizontal", "vertical"]

Ahorn.editingOptions(entity::BumperBlock) = Dict{String, Any}(
    "axes" => bumperAxes
)

Ahorn.minimumSize(entity::BumperBlock) = 24, 24
Ahorn.resizable(entity::BumperBlock) = true, true

Ahorn.selection(entity::BumperBlock) = Ahorn.getEntityRectangle(entity)

function Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::BumperBlock, room::Maple.Room)
    x, y = Ahorn.position(entity)

    width = Int(get(entity.data, "width", 32))
    height = Int(get(entity.data, "height", 32))

    axes = lowercase(get(entity.data, "axes", "both"))

    frame = frameImage[lowercase(axes)]
    faceSprite = Ahorn.getSprite(face, "Gameplay")

    tilesWidth = div(width, 8)
    tilesHeight = div(height, 8)

    Ahorn.drawRectangle(ctx, 2, 2, width - 4, height - 4, faceColor)
    Ahorn.drawImage(ctx, faceSprite, div(width - faceSprite.width, 2), div(height - faceSprite.height, 2))

    for i in 2:tilesWidth - 1
        Ahorn.drawImage(ctx, frame, (i - 1) * 8, 0, 8, 0, 8, 8)
        Ahorn.drawImage(ctx, frame, (i - 1) * 8, height - 8, 8, 24, 8, 8)
    end

    for i in 2:tilesHeight - 1
        Ahorn.drawImage(ctx, frame, 0, (i - 1) * 8, 0, 8, 8, 8)
        Ahorn.drawImage(ctx, frame, width - 8, (i - 1) * 8, 24, 8, 8, 8)
    end

    Ahorn.drawImage(ctx, frame, 0, 0, 0, 0, 8, 8)
    Ahorn.drawImage(ctx, frame, width - 8, 0, 24, 0, 8, 8)
    Ahorn.drawImage(ctx, frame, 0, height - 8, 0, 24, 8, 8)
    Ahorn.drawImage(ctx, frame, width - 8, height - 8, 24, 24, 8, 8)
end

function Ahorn.rotated(entity::BumperBlock, steps::Int)
    if abs(steps) % 2 == 1
        if entity.axes == "horizontal"
            entity.axes = "vertical"

            return entity

        elseif entity.axes == "vertical"
            entity.axes = "horizontal"

            return entity
        end
    end
end

end