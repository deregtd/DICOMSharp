/// <binding ProjectOpened='Watch - Development' />
"use strict";

var path = require('path');
var webpack = require('webpack');

var autoprefixer = require('autoprefixer');
// var ExtractTextPlugin = require('extract-text-webpack-plugin');

var plugins = [
    // new ExtractTextPlugin('./css/app.css', { allChunks: true })
];

// var svgoConfig = JSON.stringify({
//     plugins: [
//         { removeAttrs: { attrs: '(fill|stroke)' } }
//     ]
// });

var releaseBuild = process.env.RELEASEBUILD === 'yes';

if (releaseBuild) {
    plugins.push(new webpack.DefinePlugin({
        "process.env": { 
            NODE_ENV: JSON.stringify("production") 
        }
    }));
}

module.exports = {
    entry: './ts/WebViewerApp.tsx',
    output: {
        filename: './js/app.js'
    },
    plugins: plugins,
    // devtool: 'source-map',
    module: {
        rules: [{
            // Compile TS.
            test: /\.tsx?$/,
            use: 'ts-loader',
            exclude: /node_modules/,
        }, {
            test: /\.less$/,
            use: ["style-loader", "css-loader", "less-loader"],
        }, {
            test: /\.svg$/,
            loader: 'svg-inline-loader',
        }],
    },
    // postcss: function () {
    //     return [autoprefixer];
    // },
    resolve: {
        extensions: ['.ts', '.tsx', '.js']
    },
    performance: {
        hints: false,
        maxEntrypointSize: 1024000,
        maxAssetSize: 1024000
    }
};
