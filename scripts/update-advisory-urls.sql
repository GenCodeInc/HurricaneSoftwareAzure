SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRANSACTION;

DECLARE @Replacements TABLE
(
    ID int NOT NULL PRIMARY KEY,
    URL varchar(500) NOT NULL
);

INSERT INTO @Replacements (ID, URL)
VALUES
    (8,  'https://tgftp.nws.noaa.gov/data/raw/wt/wtnt21.knhc.tcm.at1.txt'),
    (9,  'https://tgftp.nws.noaa.gov/data/raw/wt/wtnt22.knhc.tcm.at2.txt'),
    (10, 'https://tgftp.nws.noaa.gov/data/raw/wt/wtnt23.knhc.tcm.at3.txt'),
    (12, 'https://tgftp.nws.noaa.gov/data/raw/wt/wtnt41.knhc.tcd.at1.txt'),
    (13, 'https://tgftp.nws.noaa.gov/data/raw/wt/wtnt42.knhc.tcd.at2.txt'),
    (14, 'https://tgftp.nws.noaa.gov/data/raw/wt/wtnt43.knhc.tcd.at3.txt'),
    (15, 'https://tgftp.nws.noaa.gov/data/raw/wt/wtnt44.knhc.tcd.at4.txt'),
    (16, 'https://tgftp.nws.noaa.gov/data/raw/wt/wtnt45.knhc.tcd.at5.txt'),
    (17, 'https://tgftp.nws.noaa.gov/data/raw/wt/wtnt31.knhc.tcp.at1.txt'),
    (18, 'https://tgftp.nws.noaa.gov/data/raw/wt/wtnt32.knhc.tcp.at2.txt'),
    (20, 'https://tgftp.nws.noaa.gov/data/raw/wt/wtnt24.knhc.tcm.at4.txt'),
    (21, 'https://tgftp.nws.noaa.gov/data/raw/wt/wtnt25.knhc.tcm.at5.txt'),
    (22, 'https://tgftp.nws.noaa.gov/data/raw/wt/wtnt33.knhc.tcp.at3.txt'),
    (23, 'https://tgftp.nws.noaa.gov/data/raw/wt/wtnt34.knhc.tcp.at4.txt'),
    (24, 'https://tgftp.nws.noaa.gov/data/raw/wt/wtnt35.knhc.tcp.at5.txt'),
    (25, 'https://tgftp.nws.noaa.gov/data/raw/ab/abnt20.knhc.two.at.txt'),
    (26, 'https://tgftp.nws.noaa.gov/data/raw/ax/axnt20.knhc.twd.at.txt'),
    (27, 'https://tgftp.nws.noaa.gov/data/raw/ab/abnt30.knhc.tws.at.txt'),
    (28, 'https://tgftp.nws.noaa.gov/data/raw/ab/abpz20.knhc.two.ep.txt'),
    (29, 'https://tgftp.nws.noaa.gov/data/raw/ax/axpz20.knhc.twd.ep.txt'),
    (30, 'https://tgftp.nws.noaa.gov/data/raw/ab/abpz30.knhc.tws.ep.txt'),
    (31, 'https://www.nhc.noaa.gov/ftp/pub/forecasts/recon/MIAREPRPD'),
    (32, 'https://www.nhc.noaa.gov/ftp/pub/forecasts/recon/MIAREPRPD'),
    (34, 'https://tgftp.nws.noaa.gov/data/raw/wt/wtpz21.knhc.tcm.ep1.txt'),
    (35, 'https://tgftp.nws.noaa.gov/data/raw/wt/wtpz22.knhc.tcm.ep2.txt'),
    (36, 'https://tgftp.nws.noaa.gov/data/raw/wt/wtpz23.knhc.tcm.ep3.txt'),
    (37, 'https://tgftp.nws.noaa.gov/data/raw/wt/wtpz24.knhc.tcm.ep4.txt'),
    (38, 'https://tgftp.nws.noaa.gov/data/raw/wt/wtpz25.knhc.tcm.ep5.txt'),
    (39, 'https://tgftp.nws.noaa.gov/data/raw/wt/wtpz41.knhc.tcd.ep1.txt'),
    (40, 'https://tgftp.nws.noaa.gov/data/raw/wt/wtpz42.knhc.tcd.ep2.txt'),
    (41, 'https://tgftp.nws.noaa.gov/data/raw/wt/wtpz43.knhc.tcd.ep3.txt'),
    (42, 'https://tgftp.nws.noaa.gov/data/raw/wt/wtpz44.knhc.tcd.ep4.txt'),
    (43, 'https://tgftp.nws.noaa.gov/data/raw/wt/wtpz45.knhc.tcd.ep5.txt'),
    (44, 'https://tgftp.nws.noaa.gov/data/raw/wt/wtpz31.knhc.tcp.ep1.txt'),
    (45, 'https://tgftp.nws.noaa.gov/data/raw/wt/wtpz32.knhc.tcp.ep2.txt'),
    (46, 'https://tgftp.nws.noaa.gov/data/raw/wt/wtpz33.knhc.tcp.ep3.txt'),
    (47, 'https://tgftp.nws.noaa.gov/data/raw/wt/wtpz34.knhc.tcp.ep4.txt'),
    (48, 'https://tgftp.nws.noaa.gov/data/raw/wt/wtpz35.knhc.tcp.ep5.txt');

IF EXISTS
(
    SELECT 1
    FROM @Replacements AS replacement
    LEFT JOIN dbo.Advisory AS advisory ON advisory.ID = replacement.ID
    WHERE advisory.ID IS NULL
)
BEGIN
    ;THROW 50001, 'One or more expected dbo.Advisory rows do not exist. No changes were committed.', 1;
END;

UPDATE advisory
SET
    advisory.URL = replacement.URL,
    advisory.Title = CASE
        WHEN advisory.RegionType = 1 AND advisory.AdvisoryType = 1 THEN 'Atlantic Tropical Weather Outlook'
        WHEN advisory.RegionType = 1 AND advisory.AdvisoryType = 2 THEN 'Atlantic Tropical Weather Discussion'
        WHEN advisory.RegionType = 1 AND advisory.AdvisoryType = 3 THEN 'Atlantic Tropical Weather Summary'
        WHEN advisory.RegionType = 1 AND advisory.AdvisoryType = 4 THEN CONCAT('Atlantic Forecast Advisory ', advisory.AdvisoryIndex)
        WHEN advisory.RegionType = 1 AND advisory.AdvisoryType = 5 THEN CONCAT('Atlantic Forecast Discussion ', advisory.AdvisoryIndex)
        WHEN advisory.RegionType = 1 AND advisory.AdvisoryType = 6 THEN CONCAT('Atlantic Public Advisory ', advisory.AdvisoryIndex)
        WHEN advisory.RegionType = 1 AND advisory.AdvisoryType = 7 THEN 'Atlantic Reconnaissance Plan'
        WHEN advisory.RegionType = 2 AND advisory.AdvisoryType = 1 THEN 'Eastern Pacific Tropical Weather Outlook'
        WHEN advisory.RegionType = 2 AND advisory.AdvisoryType = 2 THEN 'Eastern Pacific Tropical Weather Discussion'
        WHEN advisory.RegionType = 2 AND advisory.AdvisoryType = 3 THEN 'Eastern Pacific Tropical Weather Summary'
        WHEN advisory.RegionType = 2 AND advisory.AdvisoryType = 4 THEN CONCAT('Eastern Pacific Forecast Advisory ', advisory.AdvisoryIndex)
        WHEN advisory.RegionType = 2 AND advisory.AdvisoryType = 5 THEN CONCAT('Eastern Pacific Forecast Discussion ', advisory.AdvisoryIndex)
        WHEN advisory.RegionType = 2 AND advisory.AdvisoryType = 6 THEN CONCAT('Eastern Pacific Public Advisory ', advisory.AdvisoryIndex)
        WHEN advisory.RegionType = 2 AND advisory.AdvisoryType = 7 THEN 'Eastern Pacific Reconnaissance Plan'
        ELSE advisory.Title
    END,
    advisory.SubTitle = CASE
        WHEN advisory.RegionType = 1 AND advisory.AdvisoryType = 1 THEN 'TWOAT'
        WHEN advisory.RegionType = 1 AND advisory.AdvisoryType = 2 THEN 'TWDAT'
        WHEN advisory.RegionType = 1 AND advisory.AdvisoryType = 3 THEN 'TWSAT'
        WHEN advisory.RegionType = 1 AND advisory.AdvisoryType = 4 THEN CONCAT('TCMAT', advisory.AdvisoryIndex)
        WHEN advisory.RegionType = 1 AND advisory.AdvisoryType = 5 THEN CONCAT('TCDAT', advisory.AdvisoryIndex)
        WHEN advisory.RegionType = 1 AND advisory.AdvisoryType = 6 THEN CONCAT('TCPAT', advisory.AdvisoryIndex)
        WHEN advisory.RegionType = 1 AND advisory.AdvisoryType = 7 THEN 'REPRPD'
        WHEN advisory.RegionType = 2 AND advisory.AdvisoryType = 1 THEN 'TWOEP'
        WHEN advisory.RegionType = 2 AND advisory.AdvisoryType = 2 THEN 'TWDEP'
        WHEN advisory.RegionType = 2 AND advisory.AdvisoryType = 3 THEN 'TWSEP'
        WHEN advisory.RegionType = 2 AND advisory.AdvisoryType = 4 THEN CONCAT('TCMEP', advisory.AdvisoryIndex)
        WHEN advisory.RegionType = 2 AND advisory.AdvisoryType = 5 THEN CONCAT('TCDEP', advisory.AdvisoryIndex)
        WHEN advisory.RegionType = 2 AND advisory.AdvisoryType = 6 THEN CONCAT('TCPEP', advisory.AdvisoryIndex)
        WHEN advisory.RegionType = 2 AND advisory.AdvisoryType = 7 THEN 'REPRPD'
        ELSE advisory.SubTitle
    END
FROM dbo.Advisory AS advisory
INNER JOIN @Replacements AS replacement ON replacement.ID = advisory.ID;

IF @@ROWCOUNT <> 38
BEGIN
    ;THROW 50002, 'The number of updated dbo.Advisory rows was not 38. No changes were committed.', 1;
END;

COMMIT TRANSACTION;

SELECT
    advisory.ID,
    advisory.StormID,
    advisory.URL,
    advisory.AdvisoryIndex,
    advisory.RegionType,
    advisory.AdvisoryType,
    advisory.Title,
    advisory.SubTitle
FROM dbo.Advisory AS advisory
INNER JOIN @Replacements AS replacement ON replacement.ID = advisory.ID
ORDER BY advisory.ID;
