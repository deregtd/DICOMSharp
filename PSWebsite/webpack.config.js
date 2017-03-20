/// <binding ProjectOpened='Watch - Development' />
"use strict";

var path = require('path');
var webpack = require('webpack');

var autoprefixer = require('autoprefixer');
var ExtractTextPlugin = require('extract-text-webpack-plugin');

var svgoConfig = JSON.stringify({
    plugins: [
        { removeAttrs: { attrs: '(fill|stroke)' } }
    ]
});

var plugins = [
    new ExtractTextPlugin('./css/app.css', { allChunks: true })
];

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
    devtool: 'source-map',
    module: {
        loaders: [{
            test: /(\.tsx?|\.jsx?)$/,
            loader: 'regexp-replace',
            query: {
                match: {
                    pattern: '__decorate\\((\\[[^\\]]+\]), ([^.]*.prototype), \\"([^\\"]+)\\", null\\)\\;',
                    flags: 'g'
                },
                replaceWith: '__decorate($1, $2, $2.$3, null);'
            }
        }, {
            test: /(\.tsx?|\.jsx?)$/,
            loader: 'regexp-replace',
            query: {
                match: {
                    pattern: 'var __decorate = \\(this && this.__decorate\\) \\|\\| function \\(decorators, target, key, desc\\) {',
                    flags: 'g'
                },
                replaceWith: 'var __decorate = (this && this.__decorate) || function (decorators, target, protoMethod, desc) {\r\n' + 
                    '        var key = protoMethod; for (var k in target) { if (target[k] === protoMethod) { key = k; break; } }'
            }
        }, {
            test: /(\.js)$/,
            loader: 'regexp-replace',
            query: {
                match: {
                    pattern: '\\* \\@nosideeffects',
                    flags: 'g'
                },
                replaceWith: ''
            }
        }, {
            test: /\.jsx$/,
            loader: 'jsx-loader?harmony'
        }, {
            // Compile TS.
            test: /\.tsx?$/,
            loader: 'ts-loader'
        }, {
            test: /\.less$/,
            loader: ExtractTextPlugin.extract('style', 'css!postcss-loader!less')
        }, {
            test: /\.svg$/,
            loader: 'svg-inline!svgo?' + svgoConfig
        }],
    },
    postcss: function () {
        return [autoprefixer];
    },
    preLoaders: [
        // All output '.js' files will have any sourcemaps re-processed by 'source-map-loader'.
        { test: /\.js$/, loader: "source-map-loader" }
    ],
    resolve: {
        root: [
            path.resolve('.'),
            path.resolve('./node_modules')
        ],
        extensions: ['', '.ts', '.tsx', '.js', '.jsx']
    }
};
