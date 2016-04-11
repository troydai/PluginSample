var gulp = require('gulp');
var shell = require('gulp-shell');

gulp.task('default', ['restore', 'build']);

gulp.task('build', function() {
    return gulp.src('./src/**/project.json')
        .pipe(shell(['dotnet build <%= file.path %>']));
});

gulp.task('restore', function() {
    return gulp.src('./src/**/project.json')
        .pipe(shell(['dotnet restore <%= file.path %>']));
});