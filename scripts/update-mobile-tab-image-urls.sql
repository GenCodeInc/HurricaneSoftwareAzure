SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @Replacements TABLE
(
    MobileTabItemsID int NOT NULL PRIMARY KEY,
    MobileTabGroupID int NOT NULL,
    [Text] varchar(100) NOT NULL,
    OldURL varchar(500) NOT NULL,
    NewURL varchar(500) NOT NULL
);

INSERT INTO @Replacements
(
    MobileTabItemsID,
    MobileTabGroupID,
    [Text],
    OldURL,
    NewURL
)
VALUES
    (7,  3, 'Meteostat Visible',
        'http://www.ssd.noaa.gov/eumet/eatl/vis-l.jpg',
        'https://www.ospo.noaa.gov/eumet/eatl/vis.jpg'),
    (8,  3, 'Meteostat IR',
        'http://www.ssd.noaa.gov/eumet/eatl/ir4-l.jpg',
        'https://www.ospo.noaa.gov/eumet/eatl/ir4.jpg'),
    (9,  3, 'Meteostat Water Vapor',
        'http://www.ssd.noaa.gov/eumet/neatl/wv-l.jpg',
        'https://www.ospo.noaa.gov/eumet/neatl/wv.jpg'),
    (14, 4, 'Current',
        'http://www.ssd.noaa.gov/PS/TROP/DATA/RT/SST/ATL/20.jpg',
        'https://www.nhc.noaa.gov/tafb/sst_loop/14_atl.png'),
    (114, 1, 'Gulf Of Mexico GeoColor',
        'https://cdn.star.nesdis.noaa.gov/GOES16/ABI/SECTOR/gm/GEOCOLOR/1000x1000.jpg',
        'https://cdn.star.nesdis.noaa.gov/GOES19/ABI/SECTOR/mex/GEOCOLOR/1000x1000.jpg');

BEGIN TRY
    BEGIN TRANSACTION;

    IF EXISTS
    (
        SELECT 1
        FROM @Replacements AS replacement
        LEFT JOIN dbo.MobileTabItems AS item
            ON item.MobileTabItemsID = replacement.MobileTabItemsID
        WHERE item.MobileTabItemsID IS NULL
           OR item.MobileTabGroupID <> replacement.MobileTabGroupID
           OR item.[Text] <> replacement.[Text]
           OR item.URL NOT IN (replacement.OldURL, replacement.NewURL)
    )
    BEGIN
        RAISERROR(
            'A MobileTabItems row is missing or differs from its expected group, text, or URL. No changes were committed.',
            16,
            1
        );
    END;

    UPDATE item
    SET item.URL = replacement.NewURL
    FROM dbo.MobileTabItems AS item
    INNER JOIN @Replacements AS replacement
        ON replacement.MobileTabItemsID = item.MobileTabItemsID
    WHERE item.URL = replacement.OldURL;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0
        ROLLBACK TRANSACTION;

    THROW;
END CATCH;

SELECT
    item.MobileTabItemsID,
    item.MobileTabGroupID,
    item.[Text],
    item.ItemType,
    item.URL
FROM dbo.MobileTabItems AS item
INNER JOIN @Replacements AS replacement
    ON replacement.MobileTabItemsID = item.MobileTabItemsID
ORDER BY item.MobileTabItemsID;
