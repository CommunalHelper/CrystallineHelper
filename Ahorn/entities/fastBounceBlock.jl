module FastBounceBlock

using ..Ahorn, Maple

const blockTypes = ["Core", "Fire", "Ice"]

@mapdef Entity "vitellary/fastbounceblock" BounceBlock(x::Integer, y::Integer, width::Integer=16, height::Integer=16, blockType::String="Core")

const placements = Ahorn.PlacementDict(
    "Fast Bounce Block (Crystalline)" => Ahorn.EntityPlacement(
        BounceBlock,
        "rectangle"
    ),
)

Ahorn.editingOptions(entity::BounceBlock) = Dict{String, Any}(
    "blockType" => blockTypes
)

Ahorn.minimumSize(entity::BounceBlock) = 16, 16
Ahorn.resizable(entity::BounceBlock) = true, true

Ahorn.selection(entity::BounceBlock) = Ahorn.getEntityRectangle(entity)

# Not the prettiest code, but i stole it from ahorn so not my problem :)
function renderFastBounceBlock(ctx::Ahorn.Cairo.CairoContext, x::Number, y::Number, width::Number, height::Number, ice::Bool)
	frameResource = "objects/BumpBlockNew/fire00"
	crystalResource = "objects/BumpBlockNew/fire_center00"
	
	if ice
		frameResource = "objects/BumpBlockNew/ice00"
		crystalResource = "objects/BumpBlockNew/ice_center00"
	end
	
    crystalSprite = Ahorn.getSprite(crystalResource, "Gameplay")
    
    tilesWidth = div(width, 8)
    tilesHeight = div(height, 8)

    Ahorn.Cairo.save(ctx)

    Ahorn.rectangle(ctx, 0, 0, width, height)
    Ahorn.clip(ctx)

    for i in 0:ceil(Int, tilesWidth / 6)
        Ahorn.drawImage(ctx, frameResource, i * 48 + 8, 0, 8, 0, 48, 8)

        for j in 0:ceil(Int, tilesHeight / 6)
            Ahorn.drawImage(ctx, frameResource, i * 48 + 8, j * 48 + 8, 8, 8, 48, 48)

            Ahorn.drawImage(ctx, frameResource, 0, j * 48 + 8, 0, 8, 8, 48)
            Ahorn.drawImage(ctx, frameResource, width - 8, j * 48 + 8, 56, 8, 8, 48)
        end

        Ahorn.drawImage(ctx, frameResource, i * 48 + 8, height - 8, 8, 56, 48, 8)
    end

    Ahorn.drawImage(ctx, frameResource, 0, 0, 0, 0, 8, 8)
    Ahorn.drawImage(ctx, frameResource, width - 8, 0, 56, 0, 8, 8)
    Ahorn.drawImage(ctx, frameResource, 0, height - 8, 0, 56, 8, 8)
    Ahorn.drawImage(ctx, frameResource, width - 8, height - 8, 56, 56, 8, 8)
    
    Ahorn.drawImage(ctx, crystalSprite, div(width - crystalSprite.width, 2), div(height - crystalSprite.height, 2))

    Ahorn.restore(ctx)
end

function Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::BounceBlock, room::Maple.Room)
    x = Int(get(entity.data, "x", 0))
    y = Int(get(entity.data, "y", 0))

    width = Int(get(entity.data, "width", 32))
    height = Int(get(entity.data, "height", 32))
	
	ice = String(get(entity.data, "blockType", "Core")) == "Ice"

    renderFastBounceBlock(ctx, x, y, width, height, ice)
end

end