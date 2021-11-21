var terser = require('terser-webpack-plugin')
const path = require('path')

module.exports = {
    mode: 'development',
    entry: {
        entry: './src/index.ts'
    },
    output: {
        path: path.resolve(__dirname, '../wwwroot/js'),
        filename: 'index.bundle.js',
        library: "JSLib"
    },
    module: {
        rules: [
            { test: /\.js$/, use: 'babel-loader', exclude: /node_modules/ },
            { test: /\.ts$/, use: 'ts-loader', exclude: /node_modules/ }
        ]
    },
    optimization: {
        usedExports: false
    },
    plugins: [
        new terser()
    ]
}