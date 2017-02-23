﻿using System.Collections.Generic;
using System.IO;
using System.Linq;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Extras.Files;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Tv;

namespace NzbDrone.Core.Extras.Others
{
    public class ExistingOtherExtraImporter : ImportExistingExtraFilesBase<OtherExtraFile>
    {
        private readonly IExtraFileService<OtherExtraFile> _otherExtraFileService;
        private readonly IParsingService _parsingService;
        private readonly Logger _logger;

        public ExistingOtherExtraImporter(IExtraFileService<OtherExtraFile> otherExtraFileService,
                                          IParsingService parsingService,
                                          Logger logger)
            : base(otherExtraFileService)
        {
            _otherExtraFileService = otherExtraFileService;
            _parsingService = parsingService;
            _logger = logger;
        }

        public override int Order => 2;

        public override IEnumerable<ExtraFile> ProcessFiles(Movie movie, List<string> filesOnDisk, List<string> importedFiles)
        {
            _logger.Debug("Looking for existing extra files in {0}", movie.Path);

            var extraFiles = new List<OtherExtraFile>();
            var filterResult = FilterAndClean(movie, filesOnDisk, importedFiles);

            foreach (var possibleExtraFile in filterResult.FilesOnDisk)
            {
                var localMovie = _parsingService.GetLocalMovie(possibleExtraFile, movie);

                if (localMovie == null)
                {
                    _logger.Debug("Unable to parse extra file: {0}", possibleExtraFile);
                    continue;
                }

                if (localMovie.Movie == null)
                {
                    _logger.Debug("Cannot find related movie for: {0}", possibleExtraFile);
                    continue;
                }

                //if (localMovie.Episodes.DistinctBy(e => e.EpisodeFileId).Count() > 1)
                //{
                //    _logger.Debug("Extra file: {0} does not match existing files.", possibleExtraFile);
                //    continue;
                //}

                var extraFile = new OtherExtraFile
                {
                    MovieId = movie.Id,
                    MovieFileId = localMovie.Movie.MovieFileId,
                    RelativePath = movie.Path.GetRelativePath(possibleExtraFile),
                    Extension = Path.GetExtension(possibleExtraFile)
                };

                extraFiles.Add(extraFile);
            }

            _logger.Info("Found {0} existing other extra files", extraFiles.Count);
            _otherExtraFileService.Upsert(extraFiles);

            // Return files that were just imported along with files that were
            // previously imported so previously imported files aren't imported twice

            return extraFiles.Concat(filterResult.PreviouslyImported);
        }
    }
}
