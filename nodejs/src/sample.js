const S3 = require('aws-sdk/clients/s3')

const file = 'sample.txt'
const objectKey = 'objectkey'
const copyObjectKey = 'objectkeycopy'
const bucketParams = { Bucket: '<bucket_name>' } // <--- заменить
const uploadParams = { Bucket: bucketParams.Bucket, Key: '', Body: '' }
const createParams = { Bucket: bucketParams.Bucket, Key: objectKey, Body: 'test_body123' }
const metaParams = { Bucket: bucketParams.Bucket, Key: objectKey }
const copyParams = { Bucket: bucketParams.Bucket, CopySource: `${bucketParams.Bucket}/${objectKey}`, Key: copyObjectKey }

console.log('Создание клиента')
const s3 = new S3({
  accessKeyId: '<account_name>', // <--- заменить
  secretAccessKey: '<secret_key>', // <--- заменить
  endpoint: 'https://s3.timeweb.com',
  s3ForcePathStyle: true,
  region: 'ru-1',
  apiVersion: 'latest',
})

const runTest = async () => {
  try {
    console.log('Создание бакета')

    const res = await s3.createBucket(bucketParams).promise()
    console.log('Success', res)
  } catch (e) {
    console.log('Error', e)
  }

  try {
    console.log('Метаданные бакета')

    const res = await s3.headBucket(bucketParams).promise()
    console.log('Success', res)
  } catch (e) {
    console.log('Error', e)
  }

  try {
    console.log('Регион бакета')

    const res = await s3.getBucketLocation(bucketParams).promise()
    console.log('Success', res)
  } catch (e) {
    console.log('Error', e)
  }

  try {
    console.log('Список бакетов')

    const res = await s3.listBuckets().promise()
    console.log('Success', res.Buckets)
  } catch (e) {
    console.log('Error', e)
  }

  try {
    console.log('Создание файла из скрипта')

    const res = await s3.putObject(createParams).promise()
    console.log('Success', res)
  } catch (e) {
    console.log('Error', e)
  }

  try {
    console.log('Загрузка файла в бакет')

    const fs = require('fs')
    const fileStream = fs.createReadStream(file)
    fileStream.on('error', function (err) {
      console.log('File Error', err)
    })
    uploadParams.Body = fileStream
    const path = require('path')
    uploadParams.Key = path.basename(file)

    const res = await s3.upload(uploadParams).promise()
    console.log('Success', res)
  } catch (e) {
    console.log('Error', e)
  }

  try {
    console.log('Список объектов в бакете')

    const res = await s3.listObjects(bucketParams).promise()
    console.log('Success', res)
  } catch (e) {
    console.log('Error', e)
  }

  try {
    console.log('Метаданные объекта')

    const res = await s3.headObject(metaParams).promise()
    console.log('Success', res)
  } catch (e) {
    console.log('Error', e)
  }

  try {
    console.log('Копирование объекта')

    const res = await s3.copyObject(copyParams).promise()
    console.log('Success', res)
  } catch (e) {
    console.log('Error', e)
  }

  try {
    console.log('Чтение файла')

    const res = await s3.getObject(metaParams).promise()
    console.log('Success', res)
  } catch (e) {
    console.log('Error', e)
  }

  try {
    console.log('Удаление объектов')

    delete uploadParams.Body
    delete copyParams.CopySource
    copyParams.Key = copyObjectKey
    const objects = [metaParams, uploadParams, copyParams]

    await Promise.all(objects.map(async obj => {
      const res = await s3.deleteObject(obj).promise()
      console.log('Success', res)
    }))
  } catch (e) {
    console.log('Error', e)
  }

  try {
    console.log('Удаление бакета')

    const res = await s3.deleteBucket(bucketParams).promise()
    console.log('Success', res)
  } catch (e) {
    console.log('Error', e)
  }
}

runTest()
  .then(_ => {
    console.log('Done')
  })
  .catch(e => {
    console.log('Error', e)
  })
