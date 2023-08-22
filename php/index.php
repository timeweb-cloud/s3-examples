<?php

require 'vendor/autoload.php';

use Aws\S3\S3Client;
use Aws\Credentials\Credentials;

$dotenv = Dotenv\Dotenv::createImmutable(__DIR__);
$dotenv->load();

$credentials = new Credentials($_ENV['AWS_ACCESS_KEY_ID'], $_ENV['AWS_SECRET_ACCESS_KEY']);

$s3 = new S3Client([
    'version' => '2006-03-01',
    'region' => $_ENV['AWS_DEFAULT_REGION'],
    'endpoint' => $_ENV['AWS_ENDPOINT'],
    'use_path_style_endpoint' => true,
    'credentials' => $credentials
]);

$multipartUploadFileName = 'test';

$createMultipartUpload = $s3->createMultipartUpload([
    'Bucket' => $_ENV['AWS_BUCKET'],
    'Key' => $multipartUploadFileName,
]);

$uploadId = $createMultipartUpload->get('UploadId');
$parts = [];

$fileParts = ['part_1', 'part_2', 'part_3'];

foreach ($fileParts as $key => $filePart) {
    $partKey = $key + 1;
    $uploadPart = $s3->uploadPart([
        'Bucket' => $_ENV['AWS_BUCKET'],
        'Body' => $filePart,
        'Key' => $multipartUploadFileName,
        'PartNumber' => $partKey,
        'UploadId' => $uploadId,
    ]);

    array_push($parts, [
        'ETag' => $uploadPart->get('ETag'),
        'PartNumber' => $partKey
    ]);
}

$s3->completeMultipartUpload([
    'Bucket' => $_ENV['AWS_BUCKET'],
    'Key' => $multipartUploadFileName,
    'MultipartUpload' => [
        'Parts' => $parts
    ],
    'UploadId' => $uploadId
]);

$result = $s3->listObjects([
    'Bucket' => $_ENV['AWS_BUCKET']
]);

var_dump($result);
